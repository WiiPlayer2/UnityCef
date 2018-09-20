using System.Collections;
using UnityEngine;

[RequireComponent(typeof(WebBrowser))]
public class BrowserTesting2 : MonoBehaviour
{
    public Renderer Renderer;
    
    private WebBrowser browser;

    IEnumerator Start()
    {
        browser = GetComponent<WebBrowser>();
        yield return new WaitUntil(() => browser.Texture != null);
        Renderer.material.mainTexture = browser.Texture;
    }
}
