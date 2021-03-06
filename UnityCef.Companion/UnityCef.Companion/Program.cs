﻿using System;
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
using UnityCef.Companion.Ipc;
using UnityCef.Shared.Ipc;

namespace UnityCef.Companion
{
    class Program
    {
        private static LogicIpc ipc;
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
#if DEBUG && X86
                CefRuntime.Load("../../../../../cef_windows32");
#elif DEBUG && X64
                CefRuntime.Load("../../../../../cef_windows64");
#else
                CefRuntime.Load();
#endif

                var mainArgs = new CefMainArgs(args);
                var app = new App();

                var exitCode = CefRuntime.ExecuteProcess(mainArgs, app, IntPtr.Zero);
                ShowValue("Exit Code", exitCode);
                if (exitCode >= 0)
                    return exitCode;

                mainProcess = true;
                Console.WriteLine(">> Creating IPC connection...");
                ipc = new LogicIpc(new MessageIpc(new TcpDataIpc(false)));

                var settings = new CefSettings()
                {
                    MultiThreadedMessageLoop = true,
                    WindowlessRenderingEnabled = true,
                    LogFile = "cef.log",
                    LogSeverity = CefLogSeverity.Info,
                };

                CefRuntime.Initialize(mainArgs, settings, app, IntPtr.Zero);
                Console.WriteLine(">> Sending ready signal...");
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
            Console.WriteLine(">> Received shutdown signal...");
            exitWait.Set();
        }
    }
}
