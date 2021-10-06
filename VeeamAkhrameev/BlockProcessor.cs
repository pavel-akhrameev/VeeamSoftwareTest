using System;
using System.Security.Cryptography;

namespace VeeamAkhrameev
{
	internal class BlockProcessor
	{
		public static byte[] ProcessBlock(BlockData blockData)
		{
			byte[] hashSum;

			// Метод SHA256.Create() - это фабричный метод, предназначенный для возврата «лучшей» реализации для текущей платформы.
			// В .NET Core он всегда возвращает экземпляр SHA256Managed.
			using (SHA256 hash = SHA256.Create())
			{
				hashSum = hash.ComputeHash(blockData.Data, 0, blockData.Length);
			}

			return hashSum;
		}
	}
}
