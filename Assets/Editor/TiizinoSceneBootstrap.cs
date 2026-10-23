#if UNITY_EDITOR
using System.IO;
using Tiizino.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tiizino.EditorTools
{
    /// <summary>
    /// Creates a minimal playable prototype scene from the runtime controller.
    /// Run via Tools > Tiizino > Create Stage 01 Scene in a complete Unity project.
    /// </summary>
    public static class TiizinoSceneBootstrap
    {
        private const string SceneFolder = "Assets/_Tiizino/Scenes";
        private const string ScenePath = SceneFolder + "/Stage01_Bridge.unity";

        [MenuItem("Tools/Tiizino/Create Stage 01 Scene")]
        public static void CreateStage01Scene()
        {
            if (!Directory.Exists(SceneFolder))
                Directory.CreateDirectory(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject stage = new GameObject("Tiizino Stage 01");
            stage.AddComponent<BridgeStageController>();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Tiizino: could not save scene to " + ScenePath);
                return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            Selection.activeGameObject = stage;
            Debug.Log("Tiizino: created " + ScenePath
                + ". Open the scene and press Play to test the prototype.");
        }
    }
}
#endif
