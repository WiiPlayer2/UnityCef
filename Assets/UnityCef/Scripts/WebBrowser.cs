//#define COMPANION_DEBUG
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityCef.Shared.Ipc;
using UnityCef.Unity;
using UnityCef.Unity.Ipc;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class WebBrowser : MonoBehaviour
{
    #region Static
    private static readonly string companionPath;
    private static readonly object refCountLock = new object();
    private static int refCount = 0;
    private static LogicIpc ipc;

    static WebBrowser()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorUpdate;
        var dir = string.Format("./cef_{0}", CefPlatform);
#else
        var assembly = Assembly.GetExecutingAssembly();
        Debug.LogFormat("Assembly location: {0}", assembly.Location);
        var managedDir = Path.GetDirectoryName(assembly.Location);
        var dataDir = Path.GetDirectoryName(managedDir);
        var outDir = Path.GetDirectoryName(dataDir);
        var dir = Path.Combine(outDir, "cef");
#endif
        companionPath = Path.GetFullPath(string.Format("{0}/UnityCef.Companion.exe", dir));
    }

#if UNITY_EDITOR
    private static bool wasPlaying = false;

    private static void EditorUpdate()
    {
        if(Application.isPlaying != wasPlaying)
        {
            wasPlaying = Application.isPlaying;
            if(!wasPlaying)
            {
                Debug.Log("Stopped playing in Editor.");
                lock(refCountLock)
                {
                    StopCompanion();
                    refCount = 0;
                }
            }
        }
    }
#endif

    private static void IncRef()
    {
        lock(refCountLock)
        {
            refCount++;
            if(ipc == null)
                StartCompanion();
        }
    }

    private static void DecRef()
    {
        lock(refCountLock)
        {
            refCount--;
            if(refCount == 0)
                StopCompanion();
        }
    }

    public static string CefPlatform
    {
        get
        {
            var arch = "";
            var platform = "";

#if UNITY_EDITOR_64
            arch = "64";
#elif UNITY_EDITOR_32
            arch = "32";
#endif

#if UNITY_EDITOR_WIN
            platform = "windows";
#endif
            return string.Format("{0}{1}", platform, arch);
        }
    }

    private static void StartCompanion()
    {
        ipc = new LogicIpc(new MessageIpc(new TcpDataIpc(true)));
        ipc.IPC.IPC.WaitAsServer();

        Debug.Log("Starting companion app...");
#if !COMPANION_DEBUG
        Debug.LogFormat("UnityCef.Companion path: {0}", companionPath);
        Process.Start(companionPath).Dispose();
#else
        Debug.LogWarning("COMPANION_DEBUG is set.\nPlease start companion app separately.");
#endif
    }
    
    private static void StopCompanion()
    {
        if(ipc != null)
        {
            Debug.Log("Stopping companion app...");
            ipc.Shutdown();
            ipc.Dispose();
            ipc = null;
        }
    }
    #endregion

    public int Width = 800;
    public int Height = 600;
    public string StartUrl = "";

    public OnConsoleMessageEvent OnConsoleMessage;

    private BrowserIpc browserIpc;

    void OnEnable()
    {
        IncRef();
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return new WaitUntil(() => ipc.IsReady);

        var tex = browserIpc != null ? browserIpc.Texture : null;
        browserIpc = ipc.CreateBrowserWithIpc(this, Width, Height, StartUrl);
        browserIpc.Init(tex);
    }

    void OnDisable()
    {
        if(browserIpc != null)
        {
            browserIpc.Close();
            browserIpc.Dispose(false);
        }
        DecRef();
    }

    public Texture2D Texture
    {
        get
        {
            if(browserIpc == null)
                return null;
            return browserIpc.Texture;
        }
    }

    void Update()
    {
        if(browserIpc != null)
            browserIpc.Update();
    }

    public void Navigate(string url)
    {
        browserIpc.Navigate(url);
    }

    public void ExecuteJS(string code)
    {
        browserIpc.ExecuteJS(code);
    }
}
