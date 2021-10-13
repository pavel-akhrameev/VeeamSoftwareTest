using System;

namespace VeeamAkhrameev
{
	internal interface IBlockStorage
	{
		void Initialize(int blockLength, int requiredBlocksAmount);

		BlockData GetBlockData(int blockIndex);

		int StorageSize { get; }
	}
}
