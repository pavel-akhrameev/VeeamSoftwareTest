using System;
using System.IO;
using System.Threading;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Определяет метод запуска процесса, считывающего файл по блокам указанной длины и отслеживания завершения чтения.
	/// </summary>
	internal interface IFileReader
	{
		/// <summary>
		/// Запустить поблочное чтение файла.
		/// </summary>
		/// <param name="fileInfo">Экземпляр <see>System.IO.FileInfo</see> файла, который будет прочитан.</param>
		/// <param name="blockLength">Длина блока. Файл будет поделен на блоки указанной длины.</param>
		/// <param name="cancellationToken">Экземпляр токена, уведомляющего о необходимости прекратить операцию.</param>
		void StartReadFileIntoBlockQueue(FileInfo fileInfo, int blockLength, CancellationToken cancellationToken);

		/// <summary>
		/// Блокирует вызывающий поток до завершения процесса чтения файла.
		/// </summary>
		void WaitForReadingFinished();

		/// <summary>
		/// Событие возникает, если чтение файла не возможно из-за ошибки.
		/// </summary>
		event Action FileReadFailed;
	}
}
