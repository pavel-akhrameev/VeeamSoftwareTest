using System;
using System.Collections.Generic;

namespace VeeamAkhrameev
{
	internal class BlockStorage : IBlockStorage
	{
		public const int MinimalBlockSize = 1024;
		private const int MinRamReserveMiB = 100;

		private readonly IAvailableRamChecker _availableRamChecker;
		private Dictionary<int, BlockData> _blocks;

		private int _blockLength = -1;
		private int _blockLengthMiB = -1;
		private int _requiredBlocksAmount;
		private int _lastBlockId = -1;

		public BlockStorage(IAvailableRamChecker availableRamChecker)
		{
			this._availableRamChecker = availableRamChecker;
		}

		public void Initialize(int blockLength, int requiredBlocksAmount)
		{
			if (blockLength < MinimalBlockSize)
			{
				throw new ArgumentOutOfRangeException($"blockLength", "The blockLength parameter must be not less than {MinBlockSize}");
			}

			this._blockLength = blockLength;
			const int BytesInMiB = 1048576;
			this._blockLengthMiB = blockLength / BytesInMiB;
			this._requiredBlocksAmount = requiredBlocksAmount;

			this._blocks = new Dictionary<int, BlockData>();
			AllocateRam(_requiredBlocksAmount);
		}

		public int StorageSize
		{
			get
			{
				return _lastBlockId + 1;
			}
		}

		public BlockData GetBlockData(int blockIndex)
		{
			_blocks.TryGetValue(blockIndex, out BlockData blockData);
			return blockData;
		}

		/// <remarks> Если доступной оперативной пямяти не может быть выделено столько, сколько необходимо для размещения нужного количества блоков,
		/// то создастся такое количество блоков, сколько позволит разместить система.</remarks>
		private void AllocateRam(int requiredBlocksAmount)
		{
			CreateNewBlock();

			for (var blockIndex = 1; blockIndex < requiredBlocksAmount; blockIndex++)
			{
				var availableRamMiB = _availableRamChecker.GetAvailableRam();
				if (availableRamMiB <= _blockLengthMiB + MinRamReserveMiB)
				{
					break;
				}

				try
				{
					CreateNewBlock();
				}
				catch (OutOfMemoryException)
				{
					break;
				}
			}
		}

		private BlockData CreateNewBlock()
		{
			BlockData newBlockData;

			lock (_blocks)
			{
				var newBlockId = _lastBlockId + 1;
				newBlockData = new BlockData(newBlockId, _blockLength);

				_lastBlockId = newBlockId;

				_blocks.Add(newBlockId, newBlockData);
			}

			return newBlockData;
		}
	}
}
