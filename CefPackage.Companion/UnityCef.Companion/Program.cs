using System;
using System.Threading;
using UnityCef.Shared;

namespace UnityCef.Companion
{
    class Program
    {
        static void Main(string[] args)
        {
            var ipc = new IPC();
            ipc.RegisterMethod("ping", o =>
            {
                return new object[] { 1 };
            });
            Thread.Sleep(-1);
        }
    }
}
