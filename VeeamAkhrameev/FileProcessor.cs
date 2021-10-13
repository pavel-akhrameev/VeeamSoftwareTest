using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using log4net;

namespace VeeamAkhrameev
{
	internal class FileProcessor
	{
		private const int MaxBlockAmount = Int32.MaxValue;
		private const int WaitForOperationCompleteTimeOut = 100;

		private static readonly ILog _log = LogManager.GetLogger(System.Reflection.Assembly.GetEntryAssembly(), typeof(FileProcessor).Name);

		private readonly SystemInfo _systemInfo;
		private readonly IBlockQueue _blockQueue;

		private byte[][] _fileSignature;
		private int _blockLength;
		private long _fileLength;
		private long _blocksInFile;
		private int _logicalProcessorsAmount;
		private SemaphoreSlim _threadSemaphore;
		private AutoResetEvent _blockProcessedEvent = new AutoResetEvent(false);
		private AutoResetEvent _newBlockReadedEvent = new AutoResetEvent(false);
		private volatile bool _exceptionThrown = false;

		public FileProcessor(SystemInfo systemInfo, IBlockQueue blockQueue)
		{
			this._systemInfo = systemInfo;
			this._blockQueue = blockQueue;
		}

		public void ProcessFile(string filePath, int blockLength)
		{
			this._blockLength = blockLength;

			FileInfo fileInfo;
			try
			{
				fileInfo = new FileInfo(filePath);
				this._fileLength = fileInfo.Length;
			}
			catch (Exception ex)
			{
				var message = $"Failed to read the file.";
				_log.Error(message, ex);

				return;
			}

			this._blocksInFile = (long)Math.Ceiling((float)_fileLength / _blockLength);
			if (_blocksInFile > MaxBlockAmount)
			{
				var message = $"With block length={_blockLength} is not possible to calculate file signature, due to unacceptably large blocks amount.";
				_log.Error(message);

				return;
			}

			if (_fileLength < _blockLength)
			{
				// Если файл меньше заданного размера блока - нет смысла занимать память на целый блок.
				_blockLength = Math.Max((int)_fileLength, BlockStorage.MinimalBlockSize);
			}

			this._fileSignature = new byte[(int)_blocksInFile][];

			// Для достижения наибольшей производительности нужно держать в оперативной памяти по 2 блока на каждое логическое ядро процессора (на поток).
			// Один блок используется для вычисления hash-суммы, второй блок в этот момент времени доступен для считывания из файла.
			// Если файл состоит из меньшего количества блоков указанной длины, то оперативная память выделяется на это количество блоков.
			this._logicalProcessorsAmount = _systemInfo.LogicalProcessors;
			var maxBufferBlockAmount = (int)Math.Min(_blocksInFile, _logicalProcessorsAmount * 2);

			_blockQueue.Initialize(_blockLength, maxBufferBlockAmount);

			// Запуск потока читающего блоки из файла.
			var readDataThreadStart = new ParameterizedThreadStart(this.ReadDataProcess);
			var readDataThread = new Thread(readDataThreadStart);
			readDataThread.IsBackground = true;
			readDataThread.Start(fileInfo);

			// Запуск потока генерирующего сигнатуру.
			var signatureGenerationThreadStart = new ThreadStart(this.SignatureGenerationProcess);
			var signatureGenerationThread = new Thread(signatureGenerationThreadStart);
			signatureGenerationThread.IsBackground = true;
			signatureGenerationThread.Start();

			_log.Info("Signature calculation process has been started.");

			readDataThread.Join();
			signatureGenerationThread.Join();

			WritefileSignature(_fileSignature);
		}

		protected virtual void SignatureGenerationProcess()
		{
			using (this._threadSemaphore = new SemaphoreSlim(_logicalProcessorsAmount, _logicalProcessorsAmount))
			{
				var threadCollection = new HashSet<Thread>(_logicalProcessorsAmount);

				for (int blockId = 0; blockId < _blocksInFile;)
				{
					if (_exceptionThrown)
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

						blockId++;
					}
					else
					{
						/// Ждать события завершения чтения блока.
						_newBlockReadedEvent.WaitOne(WaitForOperationCompleteTimeOut);
					}
				}

				// Отслеживание завершения всех потоков вычисляющих хеши блоков.
				foreach (var existingThread in threadCollection)
				{
					if (_exceptionThrown)
					{
						break;
					}

					existingThread.Join();
				}
			}
		}

		protected virtual void ReadDataProcess(FileInfo fileInfo)
		{
			int currentBlockNumber = 0;
			using (var fileStream = fileInfo.OpenRead())
			{
				for (; ; )
				{
					if (_exceptionThrown)
					{
						break;
					}

					var gotBlockData = _blockQueue.TryGetUnusedDataBlock(out BlockData blockData);
					if (gotBlockData)
					{
						try
						{
							int expectedReadedLength;

							var fileEnded = currentBlockNumber == _blocksInFile;
							if (!fileEnded)
							{
								var isLastBlock = currentBlockNumber == _blocksInFile - 1;
								expectedReadedLength = !isLastBlock
									? _blockLength
									: (int)(_fileLength - _blockLength * (_blocksInFile - 1));
							}
							else
							{
								expectedReadedLength = 0;
							}

							var readedLength = fileStream.Read(blockData.Data, 0, _blockLength);
							if (readedLength != expectedReadedLength)
							{
								// Такого поведения не ожидается, но если вдруг в каких-то условиях это исключение будет возникать,
								// то необходимо написать функционал, собирающий блок из нескольких фрагментов.
								throw new IOException("Block number {currentBlockId} readed length is not expected. Actual={readedLength}, expected={expectedReadedLength}.");
							}

							if (!fileEnded)
							{
								_blockQueue.MarkBlockReadyToProcess(currentBlockNumber, blockData, readedLength);
								_newBlockReadedEvent.Set();

								currentBlockNumber++;
							}
							else
							{
								break;
							}
						}
						catch (Exception ex)
						{
							var message = $"Failed to read the block number {currentBlockNumber} from file.";
							_log.Error(message, ex);

							_exceptionThrown = true;
							break;
						}
					}
					else
					{
						// Ожидание события завершения обработки очередного блока.
						_blockProcessedEvent.WaitOne(WaitForOperationCompleteTimeOut);
					}
				}
			}
		}

		private void ReadDataProcess(object fileInfoObject)
		{
			var fileInfo = (FileInfo)fileInfoObject;
			try
			{
				ReadDataProcess(fileInfo);
			}
			catch (Exception ex)
			{
				var message = $"Failed to read the file.";
				_log.Error(message, ex);

				_exceptionThrown = true;
			}
		}

		protected virtual void BlockSignatureProcess(Block block)
		{
			try
			{
				var hashSum = BlockProcessor.ProcessBlock(block.BlockData);

				lock (_fileSignature)
				{
					_fileSignature[block.Number] = hashSum; // Вставить hash-сумму блока в сигнатуру файла.
				}

				_blockQueue.MarkBlockProcessed(block);
				_blockProcessedEvent.Set();
			}
			catch (Exception ex)
			{
				var message = $"Failed to calculate hash of the block number {block.Number}.";
				_log.Error(message, ex);

				_exceptionThrown = true;
			}
			finally
			{
				_threadSemaphore.Release();
			}
		}

		private void BlockSignatureProcess(object blockObject)
		{
			var block = (Block)blockObject;
			BlockSignatureProcess(block);
		}

		private static void WritefileSignature(byte[][] signature)
		{
			_log.Info("File signature calculated:");

			for (int number = 0; number < signature.Length; number++)
			{
				var blockHash = signature[number];
				var blockHashString = BitConverter.ToString(blockHash).Replace("-", string.Empty);
				Console.WriteLine($"{number} {blockHashString}");
			}
		}
	}
}
