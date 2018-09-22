using System;
using System.Collections.Generic;
using System.Text;

namespace UnityCef.Shared
{
    public interface IBrowserIpc
    {
        string GetSharedName();

        void Close();
    }
}
