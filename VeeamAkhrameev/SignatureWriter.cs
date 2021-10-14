using System;

namespace VeeamAkhrameev
{
	internal class SignatureWriter : ISignatureWriter
	{
		private byte[][] _fileSignature;
		private int _blockCount;

		public void Initialize(int blockCount)
		{
			this._blockCount = blockCount;
			this._fileSignature = new byte[_blockCount][];
		}

		public void AddBlockHash(int blockNumber, byte[] hashSum)
		{
			_fileSignature[blockNumber] = hashSum; // Вставить hash-сумму блока в сигнатуру файла.
		}

		public void WriteSignature()
		{
			WriteSignature(_fileSignature);
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
