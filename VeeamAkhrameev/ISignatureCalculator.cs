using System;
using System.Threading;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Определяет метод запуска процесса генерации сигнатуры из блоков данных и отслеживания завершения генерации.
	/// </summary>
	internal interface ISignatureCalculator
	{
		/// <summary>
		/// Запустить поблочную генерацию сигнатуры.
		/// </summary>
		/// <param name="threadCount">Количество параллельных потоков для генерации.</param>
		/// <param name="blockCount">Количество блоков которые необходимо обработать для генерации сигнатуры.</param>
		/// <param name="cancellationToken">Экземпляр токена, уведомляющего о необходимости прекратить операцию.</param>
		void StartCalculateSignature(int threadCount, long blockCount, CancellationToken cancellationToken);

		/// <summary>
		/// Блокирует вызывающий поток до завершения процесса генерации сигнатуры.
		/// </summary>
		void WaitForCalculationFinished();

		/// <summary>
		/// Событие возникает, если в процессе генерации сигнатуры случилась ошибка.
		/// </summary>
		event Action CalculationFailed;
	}
}
