using System;
using System.Collections.Generic;
using System.Threading;

using log4net;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Класс выполняющий генерацию сигнатуры.
	/// </summary>
	internal class SignatureCalculator : ISignatureCalculator
	{
		private const int WaitForBlockToProcessTimeout = 100; // milliseconds.

		private static readonly ILog _log = LogManager.GetLogger(System.Reflection.Assembly.GetEntryAssembly(), typeof(SignatureCalculator).Name);

		private readonly IReadableBlockQueue _blockQueue;
		private readonly ISignatureWriter _signatureWriter;
		private readonly Object _signatureWriterLockObject = new Object();

		private int _threadCount;
		private long _blockCount;

		private Thread _signatureCalculationThread;
		private SemaphoreSlim _threadSemaphore;
		private CancellationToken _cancellationToken;

		/// <inheritdoc/>
		public event Action CalculationFailed;

		/// <param name="blockQueue">Очередь блоков, откуда будут извлекаться блоки данных для генерации сигнатуры.</param>
		/// <param name="signatureWriter">Объект для вывода сигнатуры.</param>
		public SignatureCalculator(IReadableBlockQueue blockQueue, ISignatureWriter signatureWriter)
		{
			this._blockQueue = blockQueue;
			this._signatureWriter = signatureWriter;
		}

		/// <inheritdoc/>
		public void StartCalculateSignature(int threadCount, long blockCount, CancellationToken cancellationToken)
		{
			this._threadCount = threadCount;
			this._blockCount = blockCount;
			this._cancellationToken = cancellationToken;

			// Запуск потока генерирующего сигнатуру.
			var signatureGenerationThreadStart = new ThreadStart(this.SignatureGenerationWithExceptionLogging);
			this._signatureCalculationThread = new Thread(signatureGenerationThreadStart);
			_signatureCalculationThread.IsBackground = true;
			_signatureCalculationThread.Start();
		}

		/// <inheritdoc/>
		public void WaitForCalculationFinished()
		{
			_signatureCalculationThread.Join();
		}

		private void SignatureGenerationProcess()
		{
			using (this._threadSemaphore = new SemaphoreSlim(_threadCount, _threadCount))
			{
				var threadCollection = new HashSet<Thread>(_threadCount);

				for (int blockIndex = 0; blockIndex < _blockCount;)
				{
					if (_cancellationToken.IsCancellationRequested)
					{
						break;
					}

					#region Удаление ссылок на отработанные потоки

					var endedThreads = new List<Thread>();
					foreach (var existingThread in threadCollection)
					{
						if (!existingThread.IsAlive && existingThread.ThreadState.HasFlag(ThreadState.Stopped))
						{
							endedThreads.Add(existingThread);
						}
					}

					foreach (var endedThread in endedThreads)
					{
						threadCollection.Remove(endedThread);
					}

					#endregion

					var isDequeued = _blockQueue.TryGetUnprocessedBlock(out Block blockToProcess);
					if (isDequeued)
					{
						_threadSemaphore.Wait();

						var threadStart = new ParameterizedThreadStart(this.BlockSignatureProcess);
						var thread = new Thread(threadStart);
						thread.IsBackground = true;
						threadCollection.Add(thread);
						thread.Start(blockToProcess);

						blockIndex++;
					}
					else
					{
						// Таймаут перед повторной проверкой на наличие нового блока.
						Thread.Sleep(WaitForBlockToProcessTimeout);
					}
				}

				// Отслеживание завершения всех потоков вычисляющих хеши блоков.
				foreach (var existingThread in threadCollection)
				{
					if (_cancellationToken.IsCancellationRequested)
					{
						break;
					}

					existingThread.Join();
				}
			}
		}

		private void BlockSignatureProcess(Block block)
		{
			try
			{
				var hashSum = BlockProcessor.ProcessBlock(block.BlockData);

				lock (_signatureWriterLockObject)
				{
					_signatureWriter.AddBlockHash(block.Number, hashSum); // Вставить hash-сумму блока в сигнатуру файла.
				}

				_blockQueue.MarkBlockProcessed(block);
			}
			catch (Exception ex)
			{
				var message = $"Failed to calculate hash of the block number {block.Number}.";
				_log.Error(message, ex);

				if (CalculationFailed != null)
				{
					CalculationFailed.Invoke();
				}
			}
			finally
			{
				_threadSemaphore.Release();
			}
		}

		private void SignatureGenerationWithExceptionLogging()
		{
			try
			{
				SignatureGenerationProcess();
			}
			catch (Exception ex)
			{
				var message = "Failed to calculate signature of the file.";
				_log.Error(message, ex);

				if (CalculationFailed != null)
				{
					CalculationFailed.Invoke();
				}
			}
		}

		private void BlockSignatureProcess(object blockObject)
		{
			var block = (Block)blockObject;
			BlockSignatureProcess(block);
		}
	}
}
