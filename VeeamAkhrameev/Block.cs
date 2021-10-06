using System;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Один блок файла. Номер блока и его содержимое.
	/// </summary>
	internal class Block
	{
		private readonly BlockData _data;
		private readonly int _number;

		public Block(int number, BlockData data)
		{
			this._data = data;
			this._number = number;
		}

		public BlockData BlockData
		{
			get
			{
				return _data;
			}
		}

		/// <summary>
		/// Номер блока (для вывода в консоль).
		/// </summary>
		public int Number
		{
			get
			{
				return _number;
			}
		}
	}
}
