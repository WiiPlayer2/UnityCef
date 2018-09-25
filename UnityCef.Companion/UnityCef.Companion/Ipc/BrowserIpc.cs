using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityCef.Companion.Cef;
using UnityCef.Shared;
using UnityCef.Shared.Ipc;

namespace UnityCef.Companion.Ipc
{
    class BrowserIpc : BaseIpc, IBrowserIpc
    {
        public BrowserIpc(MessageIpc ipc, Client client)
            : base(ipc, client.Identifier.ToString())
        {
            Client = client;
        }

        public Client Client { get; private set; }

        [MessageIpc.Method]
        public void Close()
        {
            Client.Host.CloseBrowser(true);
        }

        [MessageIpc.Method]
        public void ExecuteJS(string code)
        {
            Client.MainFrame.ExecuteJavaScript(code, "", 1);
        }

        [MessageIpc.Method]
        public string GetSharedName()
        {
            return Client.RenderHandler.SharedName;
        }

        [MessageIpc.Method]
        public void Navigate(string url)
        {
            Client.MainFrame.LoadUrl(url);
        }

        public void OnConsoleMessage(LogLevel level, string message, string source, int line)
        {
            IPC.Send(MethodName(), level, message, source, line);
        }

        [MessageIpc.Method]
        public void SetFramerate(int framerate)
        {
            Client.Host.SetWindowlessFrameRate(framerate);
        }
    }
}
