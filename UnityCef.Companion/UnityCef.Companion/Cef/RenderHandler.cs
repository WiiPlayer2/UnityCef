using SharedMemory;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Xilium.CefGlue;

namespace UnityCef.Companion.Cef
{
    class RenderHandler : CefRenderHandler
    {
        private UnityCef.Shared.SharedBuffer sharedBuffer;
        //private SharedArray<byte> sharedBuffer;
        private readonly Guid sharedMemGuid = Guid.NewGuid();
        private int width;
        private int height;

        private byte[] imageData;

        public RenderHandler(Client client, int width, int height)
        {
            this.width = width;
            this.height = height;
            Client = client;
            
            imageData = new byte[width * height * 4];
            //sharedBuffer = new SharedArray<byte>(sharedMemGuid.ToString(), imageData.Length);
            sharedBuffer = new Shared.SharedBuffer(sharedMemGuid.ToString(), imageData.Length);
            Console.WriteLine($">> Creating buffer {sharedMemGuid}...");
            sharedBuffer.Create();
            Console.WriteLine($">> Created buffer {sharedMemGuid}");
        }

        public Client Client { get; private set; }

        public string SharedName
        {
            get
            {
                return sharedMemGuid.ToString();
            }
        }

        protected override CefAccessibilityHandler GetAccessibilityHandler()
        {
            throw new NotImplementedException();
        }

        protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect)
        {
            return GetViewRect(browser, ref rect);
        }

        protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
        {
            return false;
        }

        protected override bool GetScreenPoint(CefBrowser browser, int viewX, int viewY, ref int screenX, ref int screenY)
        {
            screenX = viewX;
            screenY = viewY;
            return true;
        }

        protected override bool GetViewRect(CefBrowser browser, ref CefRectangle rect)
        {
            rect.X = 0;
            rect.Y = 0;
            rect.Width = width;
            rect.Height = height;
            return true;
        }

        protected override void OnCursorChange(CefBrowser browser, IntPtr cursorHandle, CefCursorType type, CefCursorInfo customCursorInfo)
        {
        }

        protected override void OnImeCompositionRangeChanged(CefBrowser browser, CefRange selectedRange, CefRectangle[] characterBounds)
        {
        }
        
        protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
        {
            this.width = width;
            this.height = height;
            Marshal.Copy(buffer, imageData, 0, imageData.Length);

            //sharedBuffer.AcquireWriteLock();
            //sharedBuffer.Write(imageData);
            //sharedBuffer.ReleaseWriteLock();

            sharedBuffer.CopyFrom(imageData);
        }

        protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
        {
        }

        protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y)
        {
        }

        public Image GetImage()
        {
            if (imageData == null)
                return null;

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            var ptr = data.Scan0;
            Marshal.Copy(imageData, 0, ptr, imageData.Length);
            bitmap.UnlockBits(data);
            return bitmap;
        }
    }
}