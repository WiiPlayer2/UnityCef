﻿using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityCef.Companion.Ipc;
using Xilium.CefGlue;

namespace UnityCef.Companion.Cef
{
    class Client : CefClient
    {
        private static Dictionary<int, Client> clients = new Dictionary<int, Client>();

        private readonly RenderHandler renderHandler;
        private readonly LifeSpanHandler lifeSpanHandler;
        private EventWaitHandle registerWait = new EventWaitHandle(false, EventResetMode.ManualReset);

        public Client(LogicIpc ipc, int renderWidth, int renderHeight)
        {
            IPC = ipc;

            renderHandler = new RenderHandler(this, renderWidth, renderHeight);
            lifeSpanHandler = new LifeSpanHandler(this);
        }

        public static Client Get(int identifier)
        {
            return clients.ContainsKey(identifier) ? clients[identifier] : null;
        }

        public LogicIpc IPC { get; private set; }

        public BrowserIpc BrowserIPC { get; private set; }

        public int Identifier { get; set; }

        public Image GetImage()
        {
            return renderHandler.GetImage();
        }

        public void WaitForRegister()
        {
            registerWait.WaitOne();
        }

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

        protected override CefRenderHandler GetRenderHandler()
        {
            return renderHandler;
        }

        protected override CefLifeSpanHandler GetLifeSpanHandler()
        {
            return lifeSpanHandler;
        }
    }
}