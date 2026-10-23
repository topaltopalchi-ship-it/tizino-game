#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tiizino.Gameplay;

namespace Tiizino.EditorTools
{
    /// <summary>Creates the playable Stage 01 scene and registers it for builds.</summary>
    public static class TiizinoSceneBuilder
    {
        private const string SceneFolder = "Assets/_Tiizino/Scenes";
        private const string ScenePath = SceneFolder + "/Stage01_Bridge.unity";

        [MenuItem("Tools/Tiizino/Create Stage 01 Scene")]
        public static void CreateStage01Scene()
        {
            EnsureFolder("Assets/_Tiizino", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject controllerObject = new GameObject("Tiizino Stage Controller");
            controllerObject.AddComponent<BridgeStageController>();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Tiizino: could not save Stage 01 scene at " + ScenePath);
                return;
            }

            RegisterSceneForBuild(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tiizino: Stage 01 scene created and enabled in Build Settings at " + ScenePath);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void RegisterSceneForBuild(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            bool found = false;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == scenePath)
                {
                    scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                    found = true;
                }
                else
                {
                    scenes.Add(scene);
                }
            }

            if (!found)
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
