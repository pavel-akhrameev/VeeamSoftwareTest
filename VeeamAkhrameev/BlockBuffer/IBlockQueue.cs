using System;

namespace VeeamAkhrameev
{
	internal interface IBlockQueue : IReadableBlockQueue, IWritableBlockQueue
	{
		void Initialize(int blockLength, int requiredBlocksAmount);
	}
}
