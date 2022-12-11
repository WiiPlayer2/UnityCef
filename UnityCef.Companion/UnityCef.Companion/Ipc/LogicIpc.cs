using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using UnityCef.Companion.Cef;
using UnityCef.Shared;
using UnityCef.Shared.Ipc;
using Xilium.CefGlue;

namespace UnityCef.Companion.Ipc
{
    public class LogicIpc : BaseIpc, ILogicIpc
    {
        private readonly Timer pingTimer = new Timer(60 * 1000);

        public LogicIpc(MessageIpc ipc)
            : base(ipc)
        {
            pingTimer.Elapsed += PingTimeout;
            pingTimer.AutoReset = true;
            pingTimer.Start();
        }

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
                WindowlessFrameRate = 30,
            };

            CefBrowserHost.CreateBrowser(info, client, settings, url);

            client.WaitForRegister();
            return client.Identifier;
        }

        [MessageIpc.Method]
        public void Ping()
        {
            pingTimer.Stop();
            pingTimer.Interval = pingTimer.Interval;
            pingTimer.Start();
        }

        private void PingTimeout(object sender, ElapsedEventArgs e)
        {
            Shutdown();
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
