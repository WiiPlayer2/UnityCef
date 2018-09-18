using System;
using System.Collections;
using System.Collections.Generic;
using UnityCef.Shared;
using UnityEngine;

public class BrowserTesting : MonoBehaviour
{
    private UnityIPC ipc;

    // Use this for initialization
    void Start ()
    {
        ipc = new UnityIPC(new IPC(new InternalPipeIPC(false)));
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => ipc.IsReady);
        Debug.Log("Ready");
        ipc.CreateBrowser(800, 800, "https://z0r.de/6830");
    }

    void OnDisable()
    {
        ipc.Shutdown();
        ipc.IPC.Dispose();
    }
}
