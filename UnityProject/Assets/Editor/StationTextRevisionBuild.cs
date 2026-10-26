using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Build the normal playable project; recording operators belong to the isolated copy.
public static class StationTextRevisionBuild
{
    public static void Run()
    {
        StationV6Audit.Snapshot();
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-station-text-build-output");
        if (index < 0 || index + 1 >= args.Length)
            throw new ArgumentException("Missing -station-text-build-output");
        string target = Path.GetFullPath(args[index + 1]);
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/LastLightStation.unity" },
            target, BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("TEXT_REVISION_BUILD " + report.summary.result + " errors=" + report.summary.totalErrors);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 2);
    }
}
