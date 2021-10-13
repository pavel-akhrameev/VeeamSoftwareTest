using System;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Хранит данные одного блока файла.
	/// </summary>
	internal class BlockData
	{
		private readonly int _id;
		private readonly byte[] _data;
		private readonly int _maxLength;
		private int _length;

		public BlockData(int id, int maxLength)
		{
			this._id = id;
			this._maxLength = maxLength;

			this._data = new byte[maxLength];
			Array.Clear(_data, 0, maxLength);

			this._length = 0;
		}

		public int Id
		{
			get
			{
				return _id;
			}
		}

		public byte[] Data
		{
			get
			{
				return _data;
			}
		}

		public int Length
		{
			get
			{
				return _length;
			}
		}

		public void SetDataLength(int newDataLength)
		{
			this._length = newDataLength;
		}
	}
}
