//#define COMPANION_DEBUG
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityCef.Shared.Ipc;
using UnityCef.Unity.Ipc;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class WebBrowser : MonoBehaviour
{
    private static readonly string companionPath;
    private static readonly object refCountLock = new object();
    private static int refCount = 0;
    private static LogicIpc ipc;

    public int Width = 800;
    public int Height = 600;
    public string StartUrl = "";

    private BrowserIpc browserIpc;

    static WebBrowser()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorUpdate;
        var dir = string.Format("./cef_{0}", CefPlatform);
#else
        var dir = "./cef";
#endif
        companionPath = Path.GetFullPath(string.Format("{0}/UnityCef.Companion.exe", dir));

        Debug.LogFormat("UnityCef.Companion path: {0}", companionPath);
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

    private static IEnumerator IncRef()
    {
        lock(refCountLock)
        {
            refCount++;
            if(ipc == null)
                yield return StartCompanion();
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

    private static IEnumerator StartCompanion()
    {
        Debug.Log("Starting companion app...");
#if !COMPANION_DEBUG
        Process.Start(companionPath).Dispose();
#else
        Debug.Log("COMPANION_DEBUG is set.\nPlease start companion app separately.");
#endif
        yield return new WaitForSeconds(1f);
        ipc = new LogicIpc(new MessageIpc(new TcpDataIpc(false)));
        yield return new WaitUntil(() => ipc.IsReady);
    }
    
    private static void StopCompanion()
    {
        if(ipc != null)
        {
            Debug.Log("Stopping companion app...");
            ipc.Shutdown();
            ipc.Dispose();
        }
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

    ~WebBrowser()
    {
        DecRef();
    }

    IEnumerator Start()
    {
        yield return IncRef();
        browserIpc = ipc.CreateBrowserWithIpc(Width, Height, StartUrl);
    }

    void Update()
    {
        if(browserIpc != null)
            browserIpc.Update();
    }
}
