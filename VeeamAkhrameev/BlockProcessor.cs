using System;
using System.Security.Cryptography;

namespace VeeamAkhrameev
{
	internal class BlockProcessor
	{
		public static byte[] ProcessBlock(byte[] blockData)
		{
			byte[] hashSum;

			using (SHA256 hash = SHA256.Create())
			{
				hashSum = hash.ComputeHash(blockData);
			}

			return hashSum;
		}


	}
}
