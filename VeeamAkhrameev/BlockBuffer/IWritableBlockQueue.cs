using System;

namespace VeeamAkhrameev
{
	internal interface IWritableBlockQueue
	{
		bool TryGetUnusedDataBlock(out BlockData blockData);

		void MarkBlockReadyToProcess(int blockNumber, BlockData blockData, int newBlockDataLength);

		void FreeUnusedBlocks();
	}
}
