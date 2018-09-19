using System;
using System.Collections.Generic;
using System.Text;

namespace UnityCef.Shared
{
    public interface IBrowserIpc
    {
        void OnPaint(int width, int height, string sharedName);
    }
}
