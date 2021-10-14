using System;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace VeeamAkhrameev
{
	class Program
	{
		private static readonly ILog _log = LogManager.GetLogger(System.Reflection.Assembly.GetEntryAssembly(), typeof(Program).Name);

		static void Main(string[] args)
		{
			#region Настройка логера

			var layout = new PatternLayout("%date{dd.MM HH:mm:ss,fff} [%thread] %-5level %logger - %message%newline");
			var appender = new ColoredConsoleAppender { Layout = layout };
			//var appender = new RollingFileAppender { File = "VeeamAkhrameev.log", Layout = layout };
			layout.ActivateOptions();
			appender.ActivateOptions();
			BasicConfigurator.Configure(appender);

			#endregion

			var isParametersValid = TryReadInputParameters(args, out string filePath, out int blockLength);
			if (!isParametersValid)
			{
				_log.Error("Command line options are not understood.");
				_log.Info("Usage: VeeamAkhrameev.exe -f=[path to the file] -b=[block length in bytes]");

				return;
			}

			#region Dependency injection

			var systemInfoProvider = new SystemInfoProvider();
			var blockStorage = new BlockStorage(systemInfoProvider);
			var blockBuffer = new BlockBuffer(blockStorage);
			var blockQueue = new BlockQueue(blockBuffer);
			var fileReader = new FileReader(blockQueue);
			var signatureWriter = new SignatureWriter();
			var signatureCalculator = new SignatureCalculator(blockQueue, signatureWriter);
			var fileProcessor = new FileProcessor(signatureCalculator, fileReader, blockQueue, signatureWriter, systemInfoProvider);

			#endregion

			try
			{
				fileProcessor.ProcessFile(filePath, blockLength);
			}
			catch (Exception exception)
			{
				_log.Fatal("Unhandled exception caused during file processing.", exception);
			}
		}

		private static bool TryReadInputParameters(string[] args, out string filePath, out int blockLength)
		{
			const string fileParameter = "-f=";
			const string blockParameter = "-b=";

			filePath = null;
			blockLength = 0;

			bool isFilePathSet = false;
			bool isBlockLengthSet = false;

			foreach (var arg in args)
			{
				if (!isFilePathSet)
				{
					var fpIndex = arg.IndexOf(fileParameter);
					if (fpIndex > -1)
					{
						var filePathIndex = fpIndex + fileParameter.Length;
						filePath = arg.Substring(filePathIndex).Trim('\"', '\'');
						isFilePathSet = true;
					}
				}

				if (!isBlockLengthSet)
				{
					var blIndex = arg.IndexOf(blockParameter);
					if (blIndex > -1)
					{
						var blockLengthIndex = blIndex + blockParameter.Length;
						var blockLengthString = arg.Substring(blockLengthIndex).Trim('\"', '\'');
						isBlockLengthSet = int.TryParse(blockLengthString, out blockLength);
					}
				}
			}

			return isFilePathSet & isBlockLengthSet;
		}
	}
}
