#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Tiizino.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tiizino.EditorTools
{
    public static class TiizinoSceneSetup
    {
        private const string SceneDirectory = "Assets/_Tiizino/Scenes";
        private const string ScenePath = SceneDirectory + "/Stage01_Bridge.unity";

        [MenuItem("Tools/Tiizino/Create Stage 01 Scene")]
        public static void CreateStage01Scene()
        {
            if (!AssetDatabase.IsValidFolder(SceneDirectory))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Tiizino"))
                    AssetDatabase.CreateFolder("Assets", "_Tiizino");
                AssetDatabase.CreateFolder("Assets/_Tiizino", "Scenes");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject stage = new GameObject("Tiizino Stage Controller");
            stage.AddComponent<BridgeStageController>();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Tiizino: could not save stage scene to " + ScenePath);
                return;
            }

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool alreadyIncluded = false;
            foreach (EditorBuildSettingsScene entry in scenes)
            {
                if (entry.path == ScenePath)
                {
                    entry.enabled = true;
                    alreadyIncluded = true;
                    break;
                }
            }

            if (!alreadyIncluded)
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Tiizino: Stage 01 scene created and added to Build Settings: " + ScenePath);
        }
    }
}
#endif
