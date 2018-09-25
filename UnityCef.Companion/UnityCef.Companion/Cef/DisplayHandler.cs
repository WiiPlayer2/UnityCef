using System;
using UnityCef.Shared;
using Xilium.CefGlue;

namespace UnityCef.Companion.Cef
{
    internal class DisplayHandler : CefDisplayHandler
    {
        public DisplayHandler(Client client)
        {
            Client = client;
        }

        public Client Client { get; private set; }

        protected override bool OnConsoleMessage(CefBrowser browser, CefLogSeverity level, string message, string source, int line)
        {
            Client.BrowserIPC.OnConsoleMessage((LogLevel)level, message, source, line);
            return false;
        }
    }
}