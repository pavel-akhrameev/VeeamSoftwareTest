using System;
using System.Diagnostics;

namespace VeeamAkhrameev
{
	internal class SystemInfoProvider : IAvailableRamChecker, IProcessorInfoProvider
	{
		private readonly int _logicalProcessors;
		private PerformanceCounter _ramCounter;

		public SystemInfoProvider()
		{
			this._logicalProcessors = Environment.ProcessorCount;
			this._ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
		}

		public int LogicalProcessors
		{
			get
			{
				return _logicalProcessors;
			}
		}

		public int GetAvailableRam()
		{
			var megabytes = Convert.ToInt32(_ramCounter.NextValue());
			return megabytes;
		}
	}
}
