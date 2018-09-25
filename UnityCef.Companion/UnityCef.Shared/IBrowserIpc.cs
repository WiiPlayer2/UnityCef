using System;
using System.Collections.Generic;
using System.Text;

namespace UnityCef.Shared
{
    public interface IBrowserIpc
    {
        void Navigate(string url);

        void ExecuteJS(string code);

        string GetSharedName();

        void SetFramerate(int framerate);

        void Close();

        void OnConsoleMessage(LogLevel level, string message, string source, int line);
    }
}
