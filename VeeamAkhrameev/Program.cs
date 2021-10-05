using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;

namespace VeeamAkhrameev
{
	class Program
	{
		static void Main(string[] args)
		{
			// Метод SHA256.Create() - это фабричный метод, предназначенный для возврата «лучшей» реализации для текущей платформы -в.NET Core он всегда возвращает экземпляр SHA256Managed.




			//SHA256Cng

			//ManualResetEventSlim

			//ResetEventS

			//ManualResetEvent bbb;


			var scheduler = new Scheduler();
			scheduler.Initialize(int.MaxValue);
		}
	}	
}
