using Xilium.CefGlue;

namespace UnityCef.Companion.Cef
{
    class App : CefApp
    {
        public App()
        {
        }

        protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
        {
            if(string.IsNullOrEmpty(processType))
            {
                commandLine.AppendSwitch("enable-media-stream", "1");
                commandLine.AppendSwitch("enable-system-flash", "1");
            }
        }
    }
}