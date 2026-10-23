using System;
using UnityEngine;

namespace Tiizino.Gameplay
{
    [Serializable]
    public class QuestionData
    {
        [TextArea(2, 4)] public string prompt;
        [Tooltip("Optional ID mapped to a prerecorded voice line, for example grade1_bridge_q1.")]
        public string audioPhraseId;
        public string[] options;
        [Min(0)] public int correctOptionIndex;
        [TextArea(1, 3)] public string explanation;
        [Min(1)] public int difficulty = 1;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(prompt)
                || options == null
                || options.Length < 2
                || correctOptionIndex < 0
                || correctOptionIndex >= options.Length)
                return false;

            for (int i = 0; i < options.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(options[i]))
                    return false;
            }

            return true;
        }
    }
}
