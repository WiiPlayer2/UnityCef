using System;
using System.Collections.Generic;
using System.Text;
using UnityCef.Companion.Cef;
using UnityCef.Shared;
using Xilium.CefGlue;

namespace UnityCef.Companion
{
    public class CompanionIPC : IIPC
    {
        public CompanionIPC(IPC ipc)
        {
            IPC = ipc ?? throw new ArgumentNullException(nameof(ipc));
            IPC.RegisterObject(this);
        }

        public IPC IPC { get; set; }

        [IPC.Method]
        public void CreateBrowser(int width, int height, string url = "")
        {
            var info = CefWindowInfo.Create();
            info.WindowlessRenderingEnabled = true;
            info.SetAsWindowless(IntPtr.Zero, true);

            var client = new Client(width, height);

            var settings = new CefBrowserSettings
            {
                JavaScript = CefState.Enabled,
                WebGL = CefState.Enabled,
                WebSecurity = CefState.Disabled,
                WindowlessFrameRate = 30,
            };

            CefBrowserHost.CreateBrowser(info, client, settings, url);
        }

        public void CreatedBrowser()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            IPC.Dispose();
        }

        public void Ready()
        {
            IPC.Send("Ready");
        }

        [IPC.Method]
        public void Shutdown()
        {
            Program.Exit();
        }
    }
}
