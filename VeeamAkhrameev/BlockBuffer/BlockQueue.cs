using System;
using System.Collections.Concurrent;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Класс реализующий очередь нумерованных блоков данных, пригодную для использования из нескольких потоков.
	/// </summary>
	internal class BlockQueue : IBlockQueue
	{
		private readonly IBlockBuffer _blockBuffer;
		private readonly ConcurrentQueue<Block> _blocks = new ConcurrentQueue<Block>();
		private readonly Object _blockBufferLockObject = new Object();

		public BlockQueue(IBlockBuffer blockBuffer)
		{
			this._blockBuffer = blockBuffer;
		}

		/// <inheritdoc/>
		public void Initialize(int blockLength, int requiredBlocksAmount)
		{
			_blockBuffer.Initialize(blockLength, requiredBlocksAmount);
		}

		/// <inheritdoc/>
		public bool TryGetUnusedDataBlock(out BlockData blockData)
		{
			bool result;
			lock (_blockBufferLockObject)
			{
				result = _blockBuffer.TryGetUnusedDataBlock(out blockData);
			}
			return result;
		}

		/// <inheritdoc/>
		public void MarkBlockReadyToProcess(int blockNumber, BlockData blockData, int newBlockDataLength)
		{
			lock (_blockBufferLockObject)
			{
				_blockBuffer.MarkBlockReadyToProcess(blockData, newBlockDataLength);
			}

			var newBlock = new Block(blockNumber, blockData);
			_blocks.Enqueue(newBlock);
		}

		/// <inheritdoc/>
		public bool TryGetUnprocessedBlock(out Block block)
		{
			var result = _blocks.TryDequeue(out block);
			return result;
		}

		/// <inheritdoc/>
		public void MarkBlockProcessed(Block block)
		{
			lock (_blockBufferLockObject)
			{
				_blockBuffer.MarkBlockProcessed(block.BlockData);
			}
		}
	}
}
