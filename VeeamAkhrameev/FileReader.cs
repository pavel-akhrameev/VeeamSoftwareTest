using System;
using System.IO;
using System.Threading;

using log4net;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Класс выполняющий поблочное чтение указанного файла. 
	/// Прочитанные блоки по порядку добавляются в очередь. 
	/// </summary>
	internal class FileReader : IFileReader
	{
		private const int WaitForEmptyBlockTimeout = 100; // milliseconds.

		private static readonly ILog _log = LogManager.GetLogger(System.Reflection.Assembly.GetEntryAssembly(), typeof(FileReader).Name);

		private readonly IWritableBlockQueue _blockQueue;

		private FileInfo _fileInfo;
		private int _blockLength;
		private long _fileLength;
		private long _blocksInFile;
		private long _lastBlockId;

		private Thread _readDataThread;
		private CancellationToken _cancellationToken;

		/// <inheritdoc/>
		public event Action FileReadFailed;

		/// <param name="blockQueue">Очередь блоков, куда будут добавлены прочитанные из файла блоки данных.</param>
		public FileReader(IWritableBlockQueue blockQueue)
		{
			this._blockQueue = blockQueue;
		}

		/// <inheritdoc/>
		public void StartReadFileIntoBlockQueue(FileInfo fileInfo, int blockLength, CancellationToken cancellationToken)
		{
			this._fileInfo = fileInfo;
			this._blockLength = blockLength;
			this._fileLength = _fileInfo.Length;
			this._blocksInFile = (long)Math.Ceiling((float)_fileLength / _blockLength);
			this._lastBlockId = _blocksInFile - 1;

			this._cancellationToken = cancellationToken;

			// Запуск потока читающего блоки из файла.
			var readDataThreadStart = new ParameterizedThreadStart(this.ReadDataProcess);
			_readDataThread = new Thread(readDataThreadStart);
			_readDataThread.IsBackground = true;
			_readDataThread.Start(_fileInfo);
		}

		/// <inheritdoc/>
		public void WaitForReadingFinished()
		{
			_readDataThread.Join();
		}

		private void ReadDataProcess(FileInfo fileInfo)
		{
			int currentBlockNumber = 0;
			using (var fileStream = fileInfo.OpenRead())
			{
				for (; ; )
				{
					if (_cancellationToken.IsCancellationRequested)
					{
						break;
					}

					var gotDataBlock = _blockQueue.TryGetUnusedDataBlock(out BlockData blockData);
					if (gotDataBlock)
					{
						try
						{
							int expectedReadedLength;

							var fileEnded = currentBlockNumber == _blocksInFile;
							if (!fileEnded)
							{
								var isLastBlock = currentBlockNumber == _lastBlockId;
								expectedReadedLength = !isLastBlock
									? _blockLength
									: (int)(_fileLength - _blockLength * _lastBlockId);
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

							if (FileReadFailed != null)
							{
								FileReadFailed.Invoke();
							}

							break;
						}
					}
					else
					{
						// Таймаут перед повторной проверкой на наличие пустого блока.
						Thread.Sleep(WaitForEmptyBlockTimeout);
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

				if (FileReadFailed != null)
				{
					FileReadFailed.Invoke();
				}
			}
		}
	}
}
