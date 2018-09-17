using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Diagnostics;
using Xilium.CefGlue;
using Debug = UnityEngine.Debug;
using Xilium.CefGlue.Interop;
using Xilium.CefGlue.Platform.Windows;

public class WebBrowser2 : MonoBehaviour
{
    private class App : CefApp
    {
        
    }

    private class Client : CefClient
    {
        private LifeSpanHandler lifeSpanHandler = new LifeSpanHandler();

        private class LifeSpanHandler : CefLifeSpanHandler
        {
            protected override void OnAfterCreated(CefBrowser browser)
            {
                Debug.Log(browser);
            }
        }

        protected override CefLifeSpanHandler GetLifeSpanHandler()
        {
            return lifeSpanHandler;
        }
    }

    private static string PathCombine(params string[] paths)
    {
        if (paths.Length == 0)
            return "";
        var output = paths[0];
        for (var i = 1; i < paths.Length; i++)
            output = Path.Combine(output, paths[i]);
        return output;
    }

    static WebBrowser2()
    {
        return;
        CefRuntime.Load("./cef_windows64");

        //var args = Environment.GetCommandLineArgs();
        var args = new string[0];
        var mainArgs = new CefMainArgs(args);
        var cefApp = new App();

        var exitCode = CefRuntime.ExecuteProcess(mainArgs, cefApp, IntPtr.Zero);
        if (exitCode != -1)
        {
            Debug.LogErrorFormat("CefRuntime.ExecuteProcess(...) return with {0}", exitCode);
            return;
        }

        var cefSettings = new CefSettings
        {
            BrowserSubprocessPath = "./Assets/UnityCef/UnityCef.Companion.exe",
            MultiThreadedMessageLoop = true,
            LogSeverity = CefLogSeverity.Verbose,
            LogFile = "cef.log",
            WindowlessRenderingEnabled = true,
            NoSandbox = true,
        };

        CefRuntime.Initialize(mainArgs, cefSettings, cefApp, IntPtr.Zero);
        Debug.Log("CefRuntime.Initialize(...) successful");
    }

    void Start()
    {
        var info = CefWindowInfo.Create();
        info.WindowlessRenderingEnabled = true;
        info.SetAsWindowless(IntPtr.Zero, true);

        var settings = new CefBrowserSettings()
        {
            
        };

        var client = new Client();

        CefBrowserHost.CreateBrowser(info, client, settings, "www.google.de", null);
    }

    void Update()
    {

    }
}
