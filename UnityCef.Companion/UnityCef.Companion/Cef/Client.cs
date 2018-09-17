using System.Drawing;
using Xilium.CefGlue;

namespace UnityCef.Companion.Cef
{
    class Client : CefClient
    {
        private RenderHandler renderHandler;

        public Client(int renderWidth, int renderHeight)
        {
            renderHandler = new RenderHandler(renderWidth, renderHeight);
        }

        public Image GetImage()
        {
            return renderHandler.GetImage();
        }

        protected override CefRenderHandler GetRenderHandler()
        {
            return renderHandler;
        }
    }
}