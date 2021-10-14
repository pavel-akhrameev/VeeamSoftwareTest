using System;
using System.IO;
using System.Threading;

namespace VeeamAkhrameev
{
	internal interface IFileReader
	{
		void StartReadFileIntoBlockQueue(FileInfo fileInfo, int blockLength, CancellationToken cancellationToken);

		void WaitForReadingFinished();

		event Action FileReadFailed;
	}
}
