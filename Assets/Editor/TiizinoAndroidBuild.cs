#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tiizino.EditorTools
{
    /// <summary>Applies repeatable Android player settings and builds a local APK.</summary>
    public static class TiizinoAndroidBuild
    {
        private const string PackageId = "com.tiizino.game";
        private const string StageScene = "Assets/_Tiizino/Scenes/Stage01_Bridge.unity";
        private const string OutputPath = "Builds/Android/Tiizino.apk";

        [MenuItem("Tools/Tiizino/Prepare Android Build")]
        public static void PrepareAndroidBuild()
        {
            ConfigurePlayerSettings();
            Debug.Log("Tiizino: Android player settings prepared. Generate the Stage 01 scene and run Validate Project Setup before building.");
        }

        [MenuItem("Tools/Tiizino/Build Android APK")]
        public static void BuildAndroidApk()
        {
            ConfigurePlayerSettings();

            if (!File.Exists(StageScene))
            {
                EditorUtility.DisplayDialog("تیزینو — صحنه موجود نیست",
                    "ابتدا از مسیر Tools > Tiizino > Create Stage 01 Scene صحنه را بسازید؛ سپس دوباره APK بگیرید.", "باشه");
                Debug.LogError("Tiizino Android build stopped: Stage 01 scene is missing at " + StageScene);
                return;
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path) && File.Exists(scene.path))
                .Select(scene => scene.path)
                .ToArray();

            if (!scenes.Contains(StageScene, StringComparer.Ordinal))
            {
                Debug.LogError("Tiizino Android build stopped: Stage 01 is not enabled in Build Settings. Run Tools > Tiizino > Create Stage 01 Scene.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Tiizino Android APK created: " + Path.GetFullPath(OutputPath)
                    + " (" + report.summary.totalSize + " bytes).");
                EditorUtility.RevealInFinder(Path.GetFullPath(OutputPath));
            }
            else
            {
                Debug.LogError("Tiizino Android build failed: " + report.summary.result
                    + ". See the Unity Console and Editor.log for details.");
            }
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "تیزینو";
            PlayerSettings.companyName = "Tiizino";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Medium);
            EditorUserBuildSettings.buildAppBundle = false;
            Debug.Log("Tiizino Android settings: package=" + PackageId
                + ", minimum API=23, architecture=ARM64, scripting=IL2CPP, output=APK.");
        }
    }
}
#endif
