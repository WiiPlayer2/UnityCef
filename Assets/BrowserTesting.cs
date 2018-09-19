using System;
using System.Collections;
using System.Collections.Generic;
using UnityCef.Shared;
using UnityEngine;
using UnityCef.Unity.Ipc;
using UnityCef.Shared.Ipc;

public class BrowserTesting : MonoBehaviour
{
    public Renderer Renderer;

    private LogicIpc ipc;
    private BrowserIpc browserIpc;

    // Use this for initialization
    IEnumerator Start ()
    {
        ipc = new LogicIpc(new MessageIpc(new TcpDataIpc(false)));

        yield return new WaitUntil(() => ipc.IsReady);
        Debug.Log("Ready");

        var id = ipc.CreateBrowser(800, 800, "https://soundcloud.com/alstroemeria-records/arcd0067-popculture-9-xfade");
        browserIpc = new BrowserIpc(ipc.IPC, id);

        yield return new WaitUntil(() => browserIpc.Texture != null);
        Renderer.material.mainTexture = browserIpc.Texture;
    }

    void Update()
    {
        if(browserIpc != null)
            browserIpc.Update();
    }

    void OnDisable()
    {
        ipc.Shutdown();
        ipc.IPC.Dispose();
    }
}
