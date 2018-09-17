using System;
using System.Collections;
using System.Collections.Generic;
using MessageLibrary;
using SimpleWebBrowser;
using UnityEngine;

public class BrowserTesting : MonoBehaviour
{
    private BrowserEngine engine;

    // Use this for initialization
    void Start ()
    {
        engine = new BrowserEngine();
        engine.OnPageLoaded += OnPageLoaded;
        engine.OnJavaScriptQuery += OnJavaScriptQuery;
        engine.OnJavaScriptDialog += OnJavaScriptDialog;
        engine.InitPlugin(800, 800, Guid.NewGuid().ToString(), 8000, "https://streamlabs.com/alert-box/v3/F0D39F0CFCEF58632062", false);

        GetComponent<Renderer>().material.mainTexture = engine.BrowserTexture;
    }

    private void OnJavaScriptDialog(string message, string prompt, DialogEventType type)
    {
        Debug.LogFormat("OnJavaScriptDialog({0}, {1}, {2})", message, prompt, type);
    }

    private void OnJavaScriptQuery(string message)
    {
        Debug.LogFormat("OnJavaScriptQuery({0})", message);
    }

    private void OnPageLoaded(string url)
    {
        Debug.LogFormat("OnPageLoaded({0})", url);
        engine.SendExecuteJSEvent("document.getElementsByTagName(\"body\")[0].style=\"background: #000000\";");
    }
    
    // Update is called once per frame
    void Update ()
    {
        engine.UpdateTexture();
    }

    void OnDisable()
    {
        engine.Shutdown();
        engine = null;
    }
}
