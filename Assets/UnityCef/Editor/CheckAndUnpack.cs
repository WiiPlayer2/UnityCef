using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using UnityCef.Unity;

[InitializeOnLoad]
public class CheckAndUpdate : IPreprocessBuildWithReport
{
    static CheckAndUpdate()
    {
        EditorApplication.update += Update;
        Check();
    }

    private static void Update()
    {
        if(!EditorApplication.isUpdating
            && !EditorApplication.isCompiling)
        {
            Check();
        }
    }

    private static void Check()
    {
        var platform = WebBrowser.CefPlatform;
        var cefDir = Path.GetFullPath(string.Format("./cef_{0}", platform));
        var hashFile = Path.Combine(cefDir, "hash");

        if(!Directory.Exists(cefDir)
            || !File.Exists(hashFile)
            || File.ReadAllText(hashFile) != Constants.HASH)
        {
            Extract(platform, cefDir);
        }
    }

    private static void Extract(string platform, string outDir)
    {
        var zipFile = Path.GetFullPath(string.Format("./Assets/UnityCef/Companion/{0}.zip", platform));
        if(!File.Exists(zipFile))
        {
            Debug.LogWarningFormat("{0} not found.\nMaybe your platform is not supported?", zipFile);
            return;
        }
        Debug.LogFormat("Unzipping {0} to {1}...", zipFile, outDir);
        var zip = new FastZip();
        zip.ExtractZip(zipFile, outDir, "");
    }

    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        var platform = report.summary.platform;
        var platformGroup = report.summary.platformGroup;
        if(platformGroup != BuildTargetGroup.Standalone)
        {
            Debug.LogWarning("UnityCef is only supported on Standalone builds.");
            return;
        }

        var cefPlatform = "";
        switch(platform)
        {
            case BuildTarget.StandaloneWindows:
                cefPlatform = "windows32";
                break;
            case BuildTarget.StandaloneWindows64:
                cefPlatform = "windows64";
                break;
            default:
                Debug.LogWarningFormat("UnityCef is not supported on {0}.", platform);
                return;
        }

        var outDir = Path.Combine(Path.GetDirectoryName(report.summary.outputPath), "cef");
        Extract(cefPlatform, outDir);
    }
}
