using System;
using System.Collections.Generic;
using System.Text;

namespace UnityCef.Shared
{
    public interface ILogicIpc
    {
        void Ready();

        int CreateBrowser(int width, int height, string url = "");

        void Shutdown();
    }
}
