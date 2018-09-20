using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
public class CheckAndUpdate : IPreprocessBuildWithReport
{
    //TODO: Implement extraction of companion binaries to project folder
    static CheckAndUpdate()
    {
    }

    public int callbackOrder { get { return 0; } }

    //TODO: Implement extraction of companion binaries to output folder
    public void OnPreprocessBuild(BuildReport report)
    {
        throw new NotImplementedException();
    }
}
