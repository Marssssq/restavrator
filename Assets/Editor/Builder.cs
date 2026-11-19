using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class Builder
{
    public static void BuildWindows()
    {
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Build");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
        string buildPath = Path.Combine(dir, "Restavrator.exe");
        BuildPlayerOptions opt = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opt);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("BUILD SUCCEEDED: " + report.summary.totalSize + " bytes");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("BUILD FAILED with " + report.summary.totalErrors + " errors");
            EditorApplication.Exit(1);
        }
    }
}
