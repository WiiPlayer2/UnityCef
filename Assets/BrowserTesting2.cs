using System.Collections;
using UnityEngine;

[RequireComponent(typeof(WebBrowser))]
public class BrowserTesting2 : MonoBehaviour
{
    public Renderer Renderer;
    public string Url = "www.google.com";
    public bool Navigate = false;
    [Multiline]
    public string Javascript = "";
    public bool Execute = false;

    private WebBrowser browser;

    IEnumerator Start()
    {
        browser = GetComponent<WebBrowser>();
        yield return new WaitUntil(() => browser.Texture != null);
        Renderer.material.mainTexture = browser.Texture;
    }

    void Update()
    {
        if(Navigate)
        {
            Navigate = false;
            browser.Navigate(Url);
        }

        if(Execute)
        {
            Execute = false;
            browser.ExecuteJS(Javascript);
        }
    }
}
