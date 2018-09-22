using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityCef.Companion.Ipc;
using Xilium.CefGlue;

namespace UnityCef.Companion.Cef
{
    class Client : CefClient
    {
        private static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        
        private EventWaitHandle registerWait = new EventWaitHandle(false, EventResetMode.ManualReset);

        public Client(LogicIpc ipc, int renderWidth, int renderHeight)
        {
            IPC = ipc;

            RenderHandler = new RenderHandler(this, renderWidth, renderHeight);
            LifeSpanHandler = new LifeSpanHandler(this);
        }

        public static Client Get(int identifier)
        {
            return clients.ContainsKey(identifier) ? clients[identifier] : null;
        }

        public LogicIpc IPC { get; private set; }

        public BrowserIpc BrowserIPC { get; private set; }

        public RenderHandler RenderHandler { get; private set; }

        public LifeSpanHandler LifeSpanHandler { get; private set; }

        public int Identifier { get; set; }

        public Image GetImage()
        {
            return RenderHandler.GetImage();
        }

        public void WaitForRegister()
        {
            registerWait.WaitOne();
        }

        #region Registration
        public void RegisterClient(int identifier)
        {
            Identifier = identifier;
            lock (clients)
                clients[identifier] = this;
            BrowserIPC = new BrowserIpc(IPC.IPC, this);
            registerWait.Set();
        }

        public void UnregisterClient()
        {
            lock (clients)
                clients.Remove(Identifier);
        }
        #endregion

        #region Overrides
        protected override CefRenderHandler GetRenderHandler()
        {
            return RenderHandler;
        }

        protected override CefLifeSpanHandler GetLifeSpanHandler()
        {
            return LifeSpanHandler;
        }
        #endregion
    }
}