using System;
using System.Threading;

namespace VeeamAkhrameev
{
	internal interface ISignatureCalculator
	{
		void StartCalculateSignature(int threadCount, long blockCount, CancellationToken cancellationToken);

		void WaitForCalculationFinished();

		event Action CalculationFailed;
	}
}
