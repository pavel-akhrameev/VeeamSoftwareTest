using System;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Определяет методы получения неиспользуемых блоков данных и их пометки как готовых к обработке.
	/// </summary>
	internal interface IWritableBlockQueue
	{
		/// <summary>
		/// Пытается взять неиспользуемый объект данных блока.
		/// </summary>
		/// <param name="blockData">Объект данных блока.</param>
		/// <returns>Значение true, если неиспользуемый объект данных блока был успешно получен,
		/// в противном случае — значение false.</returns>
		bool TryGetUnusedDataBlock(out BlockData blockData);

		/// <summary>
		/// Пометить блок данных как готовый к обработке.
		/// </summary>
		/// <param name="blockNumber">Порядковый номер блока.</param>
		/// <param name="blockData">Объект данных блока.</param>
		/// <param name="newBlockDataLength">Длина данных в блоке.</param>
		void MarkBlockReadyToProcess(int blockNumber, BlockData blockData, int newBlockDataLength);
	}
}
