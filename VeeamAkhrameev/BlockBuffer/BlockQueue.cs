using System;
using System.Collections.Concurrent;

namespace VeeamAkhrameev
{
	internal class BlockQueue : IBlockQueue
	{
		private readonly IBlockBuffer _blockBuffer;
		private readonly ConcurrentQueue<Block> _blocks = new ConcurrentQueue<Block>();

		public BlockQueue(IBlockBuffer blockBuffer)
		{
			this._blockBuffer = blockBuffer;
		}

		public void Initialize(int blockLength, int requiredBlocksAmount)
		{
			_blockBuffer.Initialize(blockLength, requiredBlocksAmount);
		}

		public bool TryGetUnusedDataBlock(out BlockData blockData)
		{
			bool result;
			lock (_blockBuffer)
			{
				result = _blockBuffer.TryGetUnusedDataBlock(out blockData);
			}
			return result;
		}

		public void MarkBlockReadyToProcess(int blockNumber, BlockData blockData, int newBlockDataLength)
		{
			lock (_blockBuffer)
			{
				_blockBuffer.MarkBlockReadyToProcess(blockData, newBlockDataLength);
			}

			var newBlock = new Block(blockNumber, blockData);
			_blocks.Enqueue(newBlock);
		}

		public bool TryGetUnprocessedBlock(out Block block)
		{
			var result = _blocks.TryDequeue(out block);
			return result;
		}

		public void MarkBlockProcessed(Block block)
		{
			lock (_blockBuffer)
			{
				_blockBuffer.MarkBlockProcessed(block.BlockData);
			}
		}

		public void FreeUnusedBlocks()
		{
			// TODO
			lock (_blockBuffer)
			{
				_blockBuffer.FreeUnusedBlocks();
			}
		}
	}
}
