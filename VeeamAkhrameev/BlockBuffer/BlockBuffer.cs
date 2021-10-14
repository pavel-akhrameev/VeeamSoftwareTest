using System;
using System.Collections.Generic;
using System.Linq;

namespace VeeamAkhrameev
{
	internal class BlockBuffer : IBlockBuffer
	{
		#region Nested enum

		private enum BlockState
		{
			Unused,
			Unprocessed
		}

		#endregion

		private readonly IBlockStorage _blockStorage;

		private Dictionary<int, BlockState> _blocksState = new Dictionary<int, BlockState>();

		public BlockBuffer(IBlockStorage blockStorage)
		{
			this._blockStorage = blockStorage;
		}

		public void Initialize(int blockLength, int requiredBlocksAmount)
		{
			_blockStorage.Initialize(blockLength, requiredBlocksAmount);
			var blockStorageSize = _blockStorage.StorageSize;

			lock (_blocksState)
			{
				// Инициализация состояния блоков в буфере.
				_blocksState = new Dictionary<int, BlockState>(blockStorageSize);
				for (int blockIndex = 0; blockIndex < blockStorageSize; blockIndex++)
				{
					_blocksState.Add(blockIndex, BlockState.Unused);
				}
			}
		}

		public bool TryGetUnusedDataBlock(out BlockData blockData)
		{
			bool result;

			lock (_blocksState)
			{
				result = _blocksState.Any(kvp => kvp.Value == BlockState.Unused);
				if (result)
				{
					var blockState = _blocksState.FirstOrDefault(kvp => kvp.Value == BlockState.Unused);
					var blockIndex = blockState.Key;

					blockData = _blockStorage.GetBlockData(blockIndex);
				}
				else
				{
					blockData = null;
				}
			}

			return result;
		}

		public void MarkBlockReadyToProcess(BlockData blockData, int newBlockDataLength)
		{
			lock (_blocksState)
			{
				_blocksState[blockData.Id] = BlockState.Unprocessed;

				blockData.SetDataLength(newBlockDataLength);
			}
		}

		public void MarkBlockProcessed(BlockData blockData)
		{
			lock (_blocksState)
			{
				_blocksState[blockData.Id] = BlockState.Unused;
			}
		}
	}
}
