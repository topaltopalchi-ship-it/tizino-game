#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tiizino.EditorTools
{
    /// <summary>Checks the files and settings needed before attempting a device build.</summary>
    public static class TiizinoProjectValidator
    {
        private const string ScenePath = "Assets/_Tiizino/Scenes/Stage01_Bridge.unity";
        private static readonly string[] QuestionPaths =
        {
            "Assets/_Tiizino/Resources/Grade1_QuestionBank.json",
            "Assets/_Tiizino/Resources/Grade2_QuestionBank.json"
        };

        [Serializable] private sealed class QuestionBank
        {
            public Question[] questions;
            public Level[] levels;
        }
        [Serializable] private sealed class Level
        {
            public int levelId;
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
            public int difficulty;
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

            for (int bankIndex = 0; bankIndex < QuestionPaths.Length; bankIndex++)
            {
                string questionPath = QuestionPaths[bankIndex];
                if (!File.Exists(questionPath))
                {
                    Debug.LogError("Tiizino setup: grade " + (bankIndex + 1)
                        + " question bank is missing at " + questionPath);
                    hasErrors = true;
                    continue;
                }

                try
                {
                    QuestionBank bank = JsonUtility.FromJson<QuestionBank>(File.ReadAllText(questionPath));
                    if (bank == null || ((bank.levels == null || bank.levels.Length == 0)
                        && (bank.questions == null || bank.questions.Length == 0)))
                    {
                        Debug.LogError("Tiizino setup: question bank has no levels or questions: " + questionPath);
                        hasErrors = true;
                        continue;
                    }

                    HashSet<string> seenPrompts = new HashSet<string>(StringComparer.Ordinal);
                    if (bank.levels != null && bank.levels.Length > 0)
                    {
                        HashSet<int> seenLevelIds = new HashSet<int>();
                        for (int i = 0; i < bank.levels.Length; i++)
                        {
                            Level level = bank.levels[i];
                            if (level == null) continue;
                            if (level.levelId < 1 || !seenLevelIds.Add(level.levelId))
                            {
                                Debug.LogError("Tiizino setup: missing or duplicate levelId at level "
                                    + (i + 1) + " in " + questionPath);
                                hasErrors = true;
                            }
                        }
                        for (int i = 0; i < bank.levels.Length; i++)
                        {
                            Level level = bank.levels[i];
                            if (level == null || string.IsNullOrWhiteSpace(level.name)
                                || string.IsNullOrWhiteSpace(level.skill)
                                || level.questions == null || level.questions.Length == 0)
                            {
                                Debug.LogError("Tiizino setup: invalid level " + (i + 1) + " in " + questionPath);
                                hasErrors = true;
                                continue;
                            }
                            for (int j = 0; j < level.questions.Length; j++)
                                if (!IsValid(level.questions[j]) || !seenPrompts.Add(NormalizePrompt(level.questions[j]?.prompt)))
                                {
                                    Debug.LogError("Tiizino setup: invalid question at level " + (i + 1)
                                        + ", question " + (j + 1) + " in " + questionPath);
                                    hasErrors = true;
                                }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < bank.questions.Length; i++)
                            if (!IsValid(bank.questions[i]) || !seenPrompts.Add(NormalizePrompt(bank.questions[i]?.prompt)))
                            {
                                Debug.LogError("Tiizino setup: invalid flat question " + (i + 1)
                                    + " in " + questionPath);
                                hasErrors = true;
                            }
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError("Tiizino setup: could not parse " + questionPath + ". " + exception.Message);
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

        private static string NormalizePrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return string.Empty;
            string normalized = prompt.Normalize(System.Text.NormalizationForm.FormKC).Trim().ToLowerInvariant();
            return string.Join(" ", normalized.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }

        private static string NormalizeOption(string option)
        {
            if (string.IsNullOrWhiteSpace(option)) return string.Empty;
            string normalized = option.Normalize(System.Text.NormalizationForm.FormKC).Trim();
            return string.Join(" ", normalized.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }

        private static bool IsValid(Question question)
        {
            if (question == null || question.options == null || question.options.Length != 3
                || question.difficulty < 1 || question.difficulty > 3) return false;
            if (string.Equals(NormalizeOption(question.options[0]), NormalizeOption(question.options[1]), StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeOption(question.options[0]), NormalizeOption(question.options[2]), StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeOption(question.options[1]), NormalizeOption(question.options[2]), StringComparison.OrdinalIgnoreCase)) return false;
            return !string.IsNullOrWhiteSpace(question.prompt)
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
