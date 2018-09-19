using Xilium.CefGlue;

namespace UnityCef.Companion.Cef
{
    internal class LifeSpanHandler : CefLifeSpanHandler
    {
        public LifeSpanHandler(Client client)
        {
            Client = client;
        }

        public Client Client { get; private set; }

        public CefBrowser Browser { get; private set; }

        protected override bool DoClose(CefBrowser browser)
        {
            Client.UnregisterClient();
            return base.DoClose(browser);
        }

        protected override void OnAfterCreated(CefBrowser browser)
        {
            base.OnAfterCreated(browser);
            Browser = browser;
            Client.RegisterClient(browser.Identifier);
        }
    }
}