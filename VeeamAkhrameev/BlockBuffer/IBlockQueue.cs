using System;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Определяет методы работы с очередью нумерованных блоков данных.
	/// </summary>
	internal interface IBlockQueue : IReadableBlockQueue, IWritableBlockQueue
	{
		/// <summary>
		/// Инициализация экземпляра очереди.
		/// </summary>
		/// <param name="blockLength">Длина блока данных.</param>
		/// <param name="requiredBlocksAmount">Желательное количество блоков для размещения in-memory.</param>
		void Initialize(int blockLength, int requiredBlocksAmount);
	}
}
