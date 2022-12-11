//#define COMPANION_DEBUG
#if NET_4_6 || NET_STANDARD_2_0
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
#if UNITY_64
    private const string ARCH = "x64";
#endif
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private const string PLATFORM = "win";
#endif

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

    public static string CefPlatform => $"{PLATFORM}-{ARCH}";

    private static void StartCompanion()
    {
        ipc = new LogicIpc(new MessageIpc(new TcpDataIpc(true)));
        ipc.IPC.IPC.WaitAsServer();

        Debug.Log("Starting companion app...");
#if !COMPANION_DEBUG
        Debug.LogFormat("UnityCef.Companion path: {0}", companionPath);
        var psi = new ProcessStartInfo(companionPath)
        {
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        Process.Start(psi).Dispose();
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
        StartCoroutine(Ping());
    }

    private IEnumerator Init()
    {
        yield return new WaitUntil(() => ipc.IsReady);

        var tex = browserIpc != null ? browserIpc.Texture : null;
        browserIpc = ipc.CreateBrowserWithIpc(this, Width, Height, StartUrl);
        browserIpc.Init(tex);
    }

    private IEnumerator Ping()
    {
        while(true)
        {
            ipc.Ping();
            yield return new WaitForSeconds(30);
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
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
#endif
