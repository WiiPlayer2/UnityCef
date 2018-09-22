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
            Client.LifeSpanHandler.Browser.GetHost().CloseBrowser(true);
        }

        [MessageIpc.Method]
        public string GetSharedName()
        {
            return Client.RenderHandler.SharedName;
        }
    }
}
