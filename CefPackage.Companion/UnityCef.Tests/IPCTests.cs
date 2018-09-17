using System;
using System.Threading.Tasks;
using UnityCef.Shared;
using Xunit;

namespace UnityCef.Tests
{
    public class IPCTests : IDisposable
    {
        private IPC ipc;

        public IPCTests()
        {
            ipc = new IPC();
        }

        public void Dispose()
        {
            ipc.Dispose();
        }

        [Fact]
        public void Ping()
        {
            ipc.RegisterMethod("ping", new Func<int>(() => 1));
            var objs = ipc.LocalCall("ping");

            Assert.Equal(1, objs[0]);
        }
    }
}
