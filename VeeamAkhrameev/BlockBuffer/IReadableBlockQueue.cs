using System;

namespace VeeamAkhrameev
{
	internal interface IReadableBlockQueue
	{
		bool TryGetUnprocessedBlock(out Block block);

		void MarkBlockProcessed(Block block);
	}
}
