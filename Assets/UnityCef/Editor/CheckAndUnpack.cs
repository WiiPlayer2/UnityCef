using System;
using System.IO;
using System.Linq;
using UnityCef.Unity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public class CheckAndUpdate : IPreprocessBuildWithReport
{
    private const string FASTZIP_NAME = "ICSharpCode.SharpZipLib.Zip.FastZip, ICSharpCode.SharpZipLib, Version=1.0.0.999, Culture=neutral, PublicKeyToken=1b03e6acf1164f73";

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
        CheckGit();

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

    private static void CheckGit()
    {
        var currDir = Path.GetFullPath("./");
        var gitDir = Path.Combine(currDir, ".git");
        var gitignore = Path.Combine(currDir, ".gitignore");
        if(File.Exists(gitDir))
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
                Debug.LogFormat("Missing entries in .gitignore. You should add the following entries in your .gitignore file:\n{0}",
                    string.Join("\n", shouldContain));
            }
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
        var fastzipType = Type.GetType(FASTZIP_NAME);
        dynamic zip = Activator.CreateInstance(fastzipType);
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
