using System;
using System.Collections.Generic;
using System.Text;
using UnityCef.Companion.Cef;
using UnityCef.Shared;
using UnityCef.Shared.Ipc;
using Xilium.CefGlue;

namespace UnityCef.Companion.Ipc
{
    public class LogicIpc : BaseIpc, ILogicIpc
    {
        public LogicIpc(MessageIpc ipc)
            : base(ipc) { }

        [MessageIpc.Method]
        public int CreateBrowser(int width, int height, string url = "")
        {
            var info = CefWindowInfo.Create();
            info.WindowlessRenderingEnabled = true;
            info.SetAsWindowless(IntPtr.Zero, true);

            var client = new Client(this, width, height);

            var settings = new CefBrowserSettings
            {
                JavaScript = CefState.Enabled,
                WebGL = CefState.Enabled,
                WebSecurity = CefState.Disabled,
                WindowlessFrameRate = 30,
            };

            CefBrowserHost.CreateBrowser(info, client, settings, url);

            client.WaitForRegister();
            return client.Identifier;
        }

        public void Ready()
        {
            IPC.Send("Ready");
        }

        [MessageIpc.Method]
        public void Shutdown()
        {
            Program.Exit();
        }
    }
}
