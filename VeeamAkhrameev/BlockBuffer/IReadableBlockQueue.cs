using System;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Определяет методы получения нумерованных блоков данных из очереди.
	/// </summary>
	internal interface IReadableBlockQueue
	{
		/// <summary>
		/// Попытаться взять из очереди готовый к обработке блок данных.
		/// </summary>
		/// <param name="block">Объект блока данных.</param>
		/// <returns>Значение true, если готовый к обработке блок был успешно получен из очереди и удален из нее,
		/// в противном случае — значение false.</returns>
		bool TryGetUnprocessedBlock(out Block block);

		/// <summary>
		/// Пометить блок данных как обработанный - пригодный для повторного использования.
		/// </summary>
		/// <param name="block">Объект блока данных.</param>
		void MarkBlockProcessed(Block block);
	}
}
