using System;
using System.Collections.Generic;
using System.Text;

namespace UnityCef.Shared
{
    public class UnityIPC : IIPC
    {
        public UnityIPC(IPC ipc)
        {
            IPC = ipc ?? throw new ArgumentNullException(nameof(ipc));
            IPC.RegisterObject(this);
        }

        public IPC IPC { get; set; }

        public bool IsReady { get; private set; }

        public void CreateBrowser(int width, int height, string url = "")
        {
            IPC.Send("CreateBrowser", width, height, url);
        }

        [IPC.Method]
        public void CreatedBrowser()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            IPC.Dispose();
        }

        [IPC.Method]
        public void Ready()
        {
            IsReady = true;
        }

        public void Shutdown()
        {
            IPC.Send("Shutdown");
        }
    }
}
