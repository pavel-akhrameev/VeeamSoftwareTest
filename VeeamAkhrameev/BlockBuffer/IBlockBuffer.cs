using System;

namespace VeeamAkhrameev
{
	internal interface IBlockBuffer
	{
		void Initialize(int blockLength, int requiredBlocksAmount);

		bool TryGetUnusedDataBlock(out BlockData blockData);

		void MarkBlockReadyToProcess(BlockData blockData, int newBlockDataLength);

		void MarkBlockProcessed(BlockData blockData);

		void FreeUnusedBlocks();
	}
}
