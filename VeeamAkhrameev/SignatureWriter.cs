using System;

namespace VeeamAkhrameev
{
	/// <summary>
	/// Класс собирающий сигнатуру для вывода в консоль.
	/// </summary>
	internal class SignatureWriter : ISignatureWriter
	{
		private byte[][] _signature;
		private int _blockCount;

		public void Initialize(int blockCount)
		{
			this._blockCount = blockCount;
			this._signature = new byte[_blockCount][];
		}

		public void AddBlockHash(int blockNumber, byte[] hashSum)
		{
			_signature[blockNumber] = hashSum; // Вставить hash-сумму блока в сигнатуру файла.
		}

		public void WriteSignature()
		{
			WriteSignature(_signature);
		}

		private static void WriteSignature(byte[][] signature)
		{
			for (int number = 0; number < signature.Length; number++)
			{
				var blockHash = signature[number];
				var blockHashString = BitConverter.ToString(blockHash).Replace("-", string.Empty);
				Console.WriteLine($"{number} {blockHashString}");
			}
		}
	}
}
