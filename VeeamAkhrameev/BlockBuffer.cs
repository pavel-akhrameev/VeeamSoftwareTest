using System;
using System.Collections.Generic;
using System.Linq;

namespace VeeamAkhrameev
{
	internal class BlockBuffer
	{
		public const int MinBlockSize = 1024;

		private readonly Dictionary<int, BlockData> _blockBuffer = new Dictionary<int, BlockData>();
		private readonly Dictionary<int, BlockState> _blockBufferState = new Dictionary<int, BlockState>();

		private int _blockLength = -1;
		private int _lastBlockId;

		public BlockBuffer()
		{
			this._lastBlockId = -1;
		}

		public BlockBuffer(int blockLength) : this()
		{
			Initialize(blockLength);
		}

		public void Initialize(int blockLength)
		{
			if (blockLength < MinBlockSize)
			{
				throw new ArgumentOutOfRangeException($"blockLength", "The blockLength parameter must be not less than {MinBlockSize}");
			}

			this._blockLength = blockLength;
		}

		public int BufferSize
		{
			get
			{
				return _lastBlockId + 1;
			}
		}

		public int MinimalBlockSize
		{
			get
			{
				return MinBlockSize;
			}
		}

		public BlockData CreateNewBlock()
		{
			var newBlockId = _lastBlockId + 1;
			var newBlockData = new BlockData(newBlockId, _blockLength);

			_lastBlockId += 1;

			lock (_blockBufferState)
			{
				_blockBuffer.Add(newBlockId, newBlockData);
				_blockBufferState.Add(newBlockId, BlockState.Unused);
			}

			return newBlockData;
		}

		public bool TryGetUnusedBlockBuffer(out BlockData blockData)
		{
			bool result;

			lock (_blockBufferState)
			{
				result = _blockBufferState.Any(kvp => kvp.Value == BlockState.Unused);
				if (result)
				{
					var blockState = _blockBufferState.FirstOrDefault(kvp => kvp.Value == BlockState.Unused);
					var blockBufferId = blockState.Key;

					blockData = _blockBuffer
						.FirstOrDefault(kvp => kvp.Key == blockBufferId)
						.Value;
				}
				else
				{
					blockData = null;
				}
			}

			return result;
		}

		public void MarkBlockInUse(BlockData blockData, int newBlockDataLength)
		{
			lock (_blockBufferState)
			{
				_blockBufferState[blockData.Id] = BlockState.Unprocessed;

				blockData.SetDataLength(newBlockDataLength);
			}
		}

		public void MarkBlockProcessed(BlockData blockData)
		{
			lock (_blockBufferState)
			{
				_blockBufferState[blockData.Id] = BlockState.Unused;
			}
		}

		private enum BlockState
		{
			Unused,
			Unprocessed
		}
	}
}
