using System;
using System.IO;
using System.Threading;
using log4net;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Класс генерирующий сигнатуру переданного файла.
	/// </summary>
	internal class FileProcessor
	{
		private const int MaxBlockAmount = Int32.MaxValue;

		private static readonly ILog _log = LogManager.GetLogger(System.Reflection.Assembly.GetEntryAssembly(), typeof(FileProcessor).Name);

		private readonly ISignatureCalculator _signatureCalculator;
		private readonly IFileReader _fileReader;
		private readonly IBlockQueue _blockQueue;
		private readonly ISignatureWriter _signatureWriter;
		private readonly IProcessorInfoProvider _processorInfoProvider;

		private int _blockLength;
		private long _fileLength;
		private long _blocksInFile;
		private int _logicalProcessorsAmount;
		private CancellationTokenSource _cancellationSource;

		public FileProcessor(
			ISignatureCalculator signatureCalculator,
			IFileReader fileReader,
			IBlockQueue blockQueue,
			ISignatureWriter signatureWriter,
			IProcessorInfoProvider processorInfoProvider)
		{
			this._signatureCalculator = signatureCalculator;
			this._fileReader = fileReader;
			this._blockQueue = blockQueue;
			this._signatureWriter = signatureWriter;
			this._processorInfoProvider = processorInfoProvider;

			_fileReader.FileReadFailed += HandleComponentFault;
			_signatureCalculator.CalculationFailed += HandleComponentFault;
		}

		/// <summary>
		/// Сгенерировать и вывести в консоль сигнатуру указанного файла.
		/// </summary>
		/// <param name="filePath">Путь к файлу для которого будет сгенерирована сигнатура.</param>
		/// <param name="blockLength">Размер блока.</param>
		public void ProcessFile(string filePath, int blockLength)
		{
			this._blockLength = blockLength > 0 ? blockLength : throw new ArgumentOutOfRangeException("blockLength");

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

			#region Checking block count in the file

			this._blocksInFile = CalculationHelper.CalculateBlockCountInFile(_fileLength, _blockLength);
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

			#endregion

			_signatureWriter.Initialize((int)_blocksInFile);

			// Для достижения наибольшей производительности нужно держать в оперативной памяти по 2 блока на каждое логическое ядро процессора (на поток).
			// Один блок используется для вычисления hash-суммы, второй блок в этот момент времени доступен для считывания из файла.
			// Если файл состоит из меньшего количества блоков указанной длины, то оперативная память выделяется на это количество блоков.
			this._logicalProcessorsAmount = _processorInfoProvider.LogicalProcessors;
			var maxBufferBlockAmount = (int)Math.Min(_blocksInFile, _logicalProcessorsAmount * 2);

			_blockQueue.Initialize(_blockLength, maxBufferBlockAmount);

			this._cancellationSource = new CancellationTokenSource();

			// Запуск потока читающего блоки из файла.
			_fileReader.StartReadFileIntoBlockQueue(fileInfo, _blockLength, _cancellationSource.Token);

			// Запуск потока генерирующего сигнатуру.
			_signatureCalculator.StartCalculateSignature(_logicalProcessorsAmount, _blocksInFile, _cancellationSource.Token);

			_log.Info("Signature calculation process has been started.");

			if (_cancellationSource.IsCancellationRequested)
			{
				return;
			}

			_fileReader.WaitForReadingFinished(); // Ожидание завершения чтения из файла.
			_signatureCalculator.WaitForCalculationFinished(); // Ожидание завершения генерации сигнатуры.

			if (_cancellationSource.IsCancellationRequested)
			{
				return;
			}

			_log.Info("File signature calculated:");
			_signatureWriter.WriteSignature(); // Вывод сигнатуры в консоль.
		}

		private void HandleComponentFault()
		{
			_cancellationSource.Cancel();
		}
	}
}
