using System;

namespace VeeamAkhrameev
{
	internal interface ISignatureWriter
	{
		void Initialize(int blockCount);

		void AddBlockHash(int blockNumber, byte[] hashSum);

		void WriteSignature();
	}
}
