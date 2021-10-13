using System;
using System.Diagnostics;

namespace VeeamAkhrameev
{
	internal class SystemInfo : IAvailableRamChecker
	{
		private int _logicalProcessors;
		private PerformanceCounter _ramCounter;

		public SystemInfo()
		{
			_logicalProcessors = Environment.ProcessorCount;
			_ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
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
