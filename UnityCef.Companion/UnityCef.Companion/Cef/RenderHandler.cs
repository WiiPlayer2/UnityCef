﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using UnityCef.Shared;
using Xilium.CefGlue;

namespace UnityCef.Companion.Cef
{
    class RenderHandler : CefRenderHandler
    {
        private SharedBuffer sharedBuffer;
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
            sharedBuffer = new SharedBuffer(sharedMemGuid.ToString(), imageData.Length);
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
            GetViewRect(browser, out var viewRect);
            rect = viewRect;
            return true;
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

        protected override void GetViewRect(CefBrowser browser, out CefRectangle rect)
        {
            rect = new CefRectangle
            {
                X = 0,
                Y = 0,
                Width = width,
                Height = height,
            };
        }

        protected override void OnImeCompositionRangeChanged(CefBrowser browser, CefRange selectedRange, CefRectangle[] characterBounds)
        {
        }
        
        protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
        {
            this.width = width;
            this.height = height;
            Marshal.Copy(buffer, imageData, 0, imageData.Length);

            sharedBuffer.CopyFrom(imageData);
        }

        protected override void OnAcceleratedPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr sharedHandle)
        {
            throw new NotImplementedException();
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