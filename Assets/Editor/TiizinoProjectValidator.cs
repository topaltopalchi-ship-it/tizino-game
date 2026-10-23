#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tiizino.EditorTools
{
    /// <summary>Checks the files and settings needed before attempting a device build.</summary>
    public static class TiizinoProjectValidator
    {
        private const string ScenePath = "Assets/_Tiizino/Scenes/Stage01_Bridge.unity";
        private const string QuestionPath =
            "Assets/_Tiizino/Resources/Stage01_FourLevels_DRAFT.json";

        [MenuItem("Tools/Tiizino/Validate Project Setup")]
        public static void ValidateProjectSetup()
        {
            bool hasErrors = false;

            if (!File.Exists("Packages/manifest.json"))
            {
                Debug.LogError("Tiizino setup: Packages/manifest.json is missing.");
                hasErrors = true;
            }

            if (!File.Exists(QuestionPath))
            {
                Debug.LogError("Tiizino setup: packaged question bank is missing at " + QuestionPath);
                hasErrors = true;
            }
            else
            {
                string json = File.ReadAllText(QuestionPath);
                if (!json.Contains("\"levels\"") && !json.Contains("\"questions\""))
                {
                    Debug.LogError("Tiizino setup: question bank contains neither levels nor questions.");
                    hasErrors = true;
                }
                if (!json.Contains("\"options\"") || !json.Contains("\"correctOptionIndex\""))
                {
                    Debug.LogError("Tiizino setup: question bank appears to lack answer options or answer indexes.");
                    hasErrors = true;
                }
            }

            bool sceneExists = File.Exists(ScenePath);
            bool sceneEnabled = false;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == ScenePath && scenes[i].enabled)
                {
                    sceneEnabled = true;
                    break;
                }
            }

            if (!sceneExists)
                Debug.LogWarning("Tiizino setup: stage scene has not been generated yet. Run Tools > Tiizino > Create Stage 01 Scene.");
            else if (!sceneEnabled)
                Debug.LogWarning("Tiizino setup: stage scene exists but is not enabled in Build Settings.");

            if (hasErrors)
                Debug.LogError("Tiizino setup validation found blocking file/configuration issues.");
            else
                Debug.Log("Tiizino setup validation passed its basic file checks. This does not compile the project or verify Android/device behavior.");
        }
    }
}
#endif
