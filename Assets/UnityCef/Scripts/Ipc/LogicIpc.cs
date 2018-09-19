using System;
using System.Collections.Generic;
using System.Text;
using UnityCef.Shared;
using UnityCef.Shared.Ipc;

namespace UnityCef.Unity.Ipc
{
    public class LogicIpc : BaseIpc, ILogicIpc
    {
        public LogicIpc(MessageIpc ipc)
            : base(ipc) { }

        public bool IsReady { get; private set; }

        public int CreateBrowser(int width, int height, string url = "")
        {
            return (int)IPC.Request("CreateBrowser", width, height, url)[0];
        }

        [MessageIpc.Method]
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
