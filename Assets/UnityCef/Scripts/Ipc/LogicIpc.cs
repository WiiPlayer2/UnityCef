using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using UnityCef.Shared;
using UnityCef.Shared.Ipc;
using UnityEngine;

namespace UnityCef.Unity.Ipc
{
    public class LogicIpc : BaseIpc, ILogicIpc
    {
        private EventWaitHandle readyWait = new EventWaitHandle(false, EventResetMode.ManualReset);

        public LogicIpc(MessageIpc ipc)
            : base(ipc) { }

        public bool IsReady { get; private set; }

        public BrowserIpc CreateBrowserWithIpc(int width, int height, string url = "")
        {
            var id = CreateBrowser(width, height, url);
            return new BrowserIpc(IPC, id, width, height);
        }

        public int CreateBrowser(int width, int height, string url = "")
        {
            return (int)IPC.Request("CreateBrowser", width, height, url)[0];
        }

        [MessageIpc.Method]
        public void Ready()
        {
            Debug.Log("UnityCef IPC is ready.");
            IsReady = true;
            readyWait.Set();
        }

        public void WaitReady()
        {
            readyWait.WaitOne();
        }

        public void Shutdown()
        {
            IPC.Send("Shutdown");
        }
    }
}
