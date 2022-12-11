using System;
using System.IO;
using System.Linq;
using UnityCef.Unity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

[InitializeOnLoad]
public class CheckAndUpdate : IPreprocessBuildWithReport
{
    static CheckAndUpdate()
    {
        Check();
        EditorApplication.projectChanged += Check;
    }

    private static void Check()
    {
        CheckGit();

        var platform = WebBrowser.CefPlatform;
        var cefDir = Path.GetFullPath($"./cef_{platform}");
        var hashFile = Path.Combine(cefDir, "hash");

        if(!Directory.Exists(cefDir)
            || !File.Exists(hashFile)
            || File.ReadAllText(hashFile) != Constants.HASH)
        {
            Extract(platform, cefDir);
        }
    }

    private static void CheckGit()
    {
        var currDir = Path.GetFullPath("./");
        var gitDir = Path.Combine(currDir, ".git");
        var gitignore = Path.Combine(currDir, ".gitignore");
        if(Directory.Exists(gitDir))
        {
            var shouldContain = new[]
            {
                "cef_*",
                "*.log",
                "blob_storage",
                "GPUCache",
            };
            var warn = true;
            if(File.Exists(gitignore))
            {
                var lines = File.ReadAllLines(gitignore);
                if(shouldContain.Aggregate(true, (acc, curr) => acc && lines.Contains(curr)))
                    warn = false;
            }
            if(warn)
            {
                Debug.LogWarningFormat("Missing entries in .gitignore. Your .gitignore file should contain the following rules:\n\n{0}\n",
                    string.Join("\n", shouldContain));
            }
        }
    }

    private static void Extract(string platform, string outDir)
    {
        var zipFile = Path.GetFullPath($"./Assets/UnityCef/Companion/{platform}.zip");
        if(!File.Exists(zipFile))
        {
            Debug.LogWarningFormat("{0} not found.\nMaybe your platform is not supported?", zipFile);
            return;
        }
        Debug.LogFormat("Unzipping {0} to {1}...", zipFile, outDir);
        var zip = new FastZip();
        zip.ExtractZip(zipFile, outDir, "");
    }

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var platform = report.summary.platform;
        var platformGroup = report.summary.platformGroup;
        if(platformGroup != BuildTargetGroup.Standalone)
        {
            Debug.LogWarning("UnityCef is only supported on Standalone builds.");
            return;
        }

        string cefPlatform;
        switch(platform)
        {
            case BuildTarget.StandaloneWindows64:
                cefPlatform = "win-x64";
                break;

            default:
                Debug.LogWarningFormat("UnityCef is not supported on {0}.", platform);
                return;
        }

        var outDir = Path.Combine(Path.GetDirectoryName(report.summary.outputPath)!, "cef");
        Extract(cefPlatform, outDir);
    }
}
