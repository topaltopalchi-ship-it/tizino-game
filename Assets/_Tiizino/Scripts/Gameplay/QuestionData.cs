using System;
using UnityEngine;

namespace Tiizino.Gameplay
{
    [Serializable]
    public class QuestionData
    {
        [TextArea(2, 4)] public string prompt;
        public string[] options;
        [Min(0)] public int correctOptionIndex;
        [TextArea(1, 3)] public string explanation;
        [Min(1)] public int difficulty = 1;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(prompt)
                && options != null
                && options.Length >= 2
                && correctOptionIndex >= 0
                && correctOptionIndex < options.Length;
        }
    }
}
