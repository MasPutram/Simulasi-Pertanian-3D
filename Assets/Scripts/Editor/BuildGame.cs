using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Build game menjadi aplikasi Windows (.exe) yang bisa dimainkan offline tanpa Unity.
public static class BuildGame
{
    const string OutputDir = "Build/Windows";
    const string ExeName = "SimulasiPertanian.exe";

    [MenuItem("Farm/Build/Windows (.exe)")]
    public static void BuildWindows()
    {
        PlayerSettings.productName = "Simulasi Pertanian";
        PlayerSettings.companyName = "Kelompok UMP";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = OutputDir + "/" + ExeName,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary s = report.summary;
        Debug.Log("[BuildGame] Hasil: " + s.result + ", ukuran " + (s.totalSize / (1024 * 1024)) + " MB, error " + s.totalErrors
                  + ", lokasi " + System.IO.Path.GetFullPath(OutputDir));

        if (Application.isBatchMode)
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }
}
