#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tiizino.EditorTools
{
    /// <summary>Checks the files and settings needed before attempting a device build.</summary>
    public static class TiizinoProjectValidator
    {
        private const string ScenePath = "Assets/_Tiizino/Scenes/Stage01_Bridge.unity";
        private const string QuestionPath = "Assets/_Tiizino/Resources/Grade1_QuestionBank.json";

        [Serializable] private sealed class QuestionBank
        {
            public Question[] questions;
            public Level[] levels;
        }
        [Serializable] private sealed class Level
        {
            public string name;
            public string skill;
            public Question[] questions;
        }
        [Serializable] private sealed class Question
        {
            public string prompt;
            public string[] options;
            public int correctOptionIndex;
            public string explanation;
        }

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
                Debug.LogError("Tiizino setup: grade 1 question bank is missing at " + QuestionPath);
                hasErrors = true;
            }
            else
            {
                try
                {
                    QuestionBank bank = JsonUtility.FromJson<QuestionBank>(File.ReadAllText(QuestionPath));
                    if (bank == null || ((bank.levels == null || bank.levels.Length == 0)
                        && (bank.questions == null || bank.questions.Length == 0)))
                    {
                        Debug.LogError("Tiizino setup: question bank has no levels or questions.");
                        hasErrors = true;
                    }
                    else if (bank.levels != null && bank.levels.Length > 0)
                    {
                        for (int i = 0; i < bank.levels.Length; i++)
                        {
                            Level level = bank.levels[i];
                            if (level == null || string.IsNullOrWhiteSpace(level.name)
                                || string.IsNullOrWhiteSpace(level.skill)
                                || level.questions == null || level.questions.Length == 0)
                            {
                                Debug.LogError("Tiizino setup: level " + (i + 1) + " is missing its name, skill, or questions.");
                                hasErrors = true;
                                continue;
                            }
                            for (int j = 0; j < level.questions.Length; j++)
                                if (!IsValid(level.questions[j]))
                                {
                                    Debug.LogError("Tiizino setup: invalid question at level " + (i + 1) + ", question " + (j + 1) + ".");
                                    hasErrors = true;
                                }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < bank.questions.Length; i++)
                            if (!IsValid(bank.questions[i]))
                            {
                                Debug.LogError("Tiizino setup: invalid flat question " + (i + 1) + ".");
                                hasErrors = true;
                            }
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError("Tiizino setup: could not parse question bank JSON. " + exception.Message);
                    hasErrors = true;
                }
            }

            bool sceneExists = File.Exists(ScenePath);
            bool sceneEnabled = false;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
                if (scenes[i].path == ScenePath && scenes[i].enabled)
                {
                    sceneEnabled = true;
                    break;
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

        private static bool IsValid(Question question)
        {
            return question != null
                && !string.IsNullOrWhiteSpace(question.prompt)
                && question.options != null
                && question.options.Length == 3
                && !string.IsNullOrWhiteSpace(question.options[0])
                && !string.IsNullOrWhiteSpace(question.options[1])
                && !string.IsNullOrWhiteSpace(question.options[2])
                && question.correctOptionIndex >= 0
                && question.correctOptionIndex < question.options.Length
                && !string.IsNullOrWhiteSpace(question.explanation);
        }
    }
}
#endif
