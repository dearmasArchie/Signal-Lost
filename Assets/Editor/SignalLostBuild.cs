using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SignalLost.EditorTools
{
    public static class SignalLostBuild
    {
        public const string WindowsBuildPath = "Builds/Windows/SignalLost.exe";

        [MenuItem("Signal Lost/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            ConfigurePlayer();

            string outputDirectory = Path.GetDirectoryName(WindowsBuildPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/Scenes/MainMenu.unity",
                    "Assets/Scenes/SignalLost.unity",
                    "Assets/Scenes/UndergroundFacility.unity"
                },
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows build failed: {summary.result}, {summary.totalErrors} error(s).");
            }

            Debug.Log(
                $"[SignalLostBuild] WINDOWS_BUILD_OK path={WindowsBuildPath} "
                + $"size={summary.totalSize} time={summary.totalTime}");
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "IT 402W";
            PlayerSettings.productName = "Project Outpost - Signal Lost";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Standalone,
                ScriptingImplementation.Mono2x);
            AssetDatabase.SaveAssets();
        }
    }
}

