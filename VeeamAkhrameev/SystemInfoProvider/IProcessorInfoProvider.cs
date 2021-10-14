using System;

namespace VeeamAkhrameev
{
	internal interface IProcessorInfoProvider
	{
		int LogicalProcessors { get; }
	}
}
