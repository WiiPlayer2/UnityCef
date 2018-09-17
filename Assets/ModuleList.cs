using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ModuleList : MonoBehaviour
{
    void Start()
    {
        var mods = Process.GetCurrentProcess().Modules;
        File.Delete("D:/tmp/modules.txt");
        using(var writer = new StreamWriter("D:/tmp/modules.txt", false))
        {
            foreach(ProcessModule m in mods)
            {
                writer.WriteLine(m);
                Debug.Log(m.ModuleName);
            }
            writer.Flush();
        }
    }
}
