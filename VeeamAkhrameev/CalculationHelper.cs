using System;

namespace VeeamAkhrameev
{
	internal static class CalculationHelper
	{
		/// <summary>
		/// Рассчитать количество блоков в файле. Если файл пустой, то он состоит из одного пустого блока.
		/// </summary>
		public static long CalculateBlockCountInFile(long fileLength, int blockSize)
		{
			var blocksInFile = Math.Max((long)Math.Ceiling((float)fileLength / blockSize), 1);
			return blocksInFile;
		}
	}
}
