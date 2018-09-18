using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xilium.CefGlue;
using UnityCef.Shared;
using UnityCef.Companion.Cef;
using System.Drawing.Imaging;

namespace UnityCef.Companion
{
    class Program
    {
        private static CompanionIPC ipc = new CompanionIPC(new IPC(new InternalPipeIPC(true)));
        private static EventWaitHandle exitWait = new EventWaitHandle(false, EventResetMode.ManualReset);

        public static void ShowValue(string name, object value, TextWriter output = null)
        {
            var fgColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            output = output ?? Console.Out;

            Console.WriteLine($"=== {name} ===");
            foreach(var s in (value ?? "<null>").ToString().Split('\n'))
            {
                Console.WriteLine($"\t{s}");
            }

            Console.ForegroundColor = fgColor;
        }

        public static void ShowValues<T>(string name, IEnumerable<T> values, TextWriter output = null)
        {
            ShowValue(name, string.Join("\n", values.Select(o => o.ToString())));
        }

        static int Main(string[] args)
        {
            ShowValues("Command Line Arguments", args);
            ShowValue("Working Directory", Environment.CurrentDirectory);

            var mainProcess = false;

            try
            {
                CefRuntime.Load("../../../../../cef_windows64");

                var mainArgs = new CefMainArgs(args);
                var app = new App();

                var exitCode = CefRuntime.ExecuteProcess(mainArgs, app, IntPtr.Zero);
                ShowValue("Exit Code", exitCode);
                if (exitCode >= 0)
                    return exitCode;

                mainProcess = true;

                var settings = new CefSettings()
                {
                    MultiThreadedMessageLoop = true,
                    WindowlessRenderingEnabled = true,
                    LogFile = "cef.log",
                    LogSeverity = CefLogSeverity.Info,
                };

                CefRuntime.Initialize(mainArgs, settings, app, IntPtr.Zero);
                ipc.Ready();

                exitWait.WaitOne();

                CefRuntime.Shutdown();

                Environment.Exit(0);
                return 0;
            }
            catch(Exception e)
            {
                ShowValue("Exception", e, Console.Error);
                return 1;
            }
            finally
            {
                if (mainProcess)
                {
                    ShowValue("Exit", "[Press any key to exit]");
                    Console.ReadKey(true);
                }
            }
        }

        public static void Exit()
        {
            exitWait.Set();
        }

        static async void Run()
        {
            await Task.Run(async () =>
            {
                var info = CefWindowInfo.Create();
                info.WindowlessRenderingEnabled = true;
                info.SetAsWindowless(IntPtr.Zero, true);

                var client = new Client(800, 600);

                var settings = new CefBrowserSettings()
                {
                    JavaScript = CefState.Enabled,
                    WebGL = CefState.Enabled,
                    WebSecurity = CefState.Disabled,
                    WindowlessFrameRate = 30,
                };

                CefBrowserHost.CreateBrowser(info, client, settings, "https://z0r.de/6830");

                while (true)
                {
                    await Task.Delay(10000);
                    Console.WriteLine("Snap!");
                    client.GetImage()?.Save(@"D:\tmp\screenshot.png", ImageFormat.Png);
                }
            });
        }
    }
}
