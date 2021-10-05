using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace VeeamAkhrameev
{
	internal class Scheduler
	{
		const int MinDataUnitLength = 8388608; // 2^23 = 8 MiB.

		private int _blockLength;
		private int _logicalProcessors;
		private int _dataUnitsPerBlock; // Parts in block.
		private int _dataUnitMaxLength;

		private PerformanceCounter _ramCounter;

		public void Initialize(int blockLength)
		{
			_blockLength = blockLength;
			_logicalProcessors = Environment.ProcessorCount;
			_ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);

			var availableRam = GetAvailableRam();
			var ramPerProcessor = availableRam / _logicalProcessors;
			var blockLengthMiB = _blockLength / 1024 / 1024;
			if (ramPerProcessor >= blockLengthMiB) // TODO: переделать эту грубую формулу. Нужен поправочный коефициент.
			{
				// Делить блок на юниты не нужно.
				_dataUnitsPerBlock = 1;
			}
			else
			{
				// Вычислить размер юнита

				_dataUnitsPerBlock = (int)Math.Ceiling((float)blockLengthMiB / ramPerProcessor);
				_dataUnitMaxLength = (int)Math.Ceiling((float)blockLengthMiB / _dataUnitsPerBlock);
			}
		}

		public int GetAvailableRam()
		{
			var megabytes = Convert.ToInt32(_ramCounter.NextValue());
			return megabytes;
		}
	}
}
