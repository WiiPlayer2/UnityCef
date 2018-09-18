using System;
using System.Collections.Generic;
using System.Text;

namespace UnityCef.Shared
{
    public interface IIPC : IDisposable
    {
        void Ready();

        void CreateBrowser(int width, int height, string url = "");

        void CreatedBrowser();

        void Shutdown();
    }
}
