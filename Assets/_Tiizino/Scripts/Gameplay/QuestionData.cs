using System;
using System.Collections.Generic;
using System.Text;
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
            if (difficulty < 1 || difficulty > 3
                || string.IsNullOrWhiteSpace(prompt)
                || options == null
                || options.Length < 2
                || correctOptionIndex < 0
                || correctOptionIndex >= options.Length
                || string.IsNullOrWhiteSpace(explanation))
                return false;

            HashSet<string> normalizedOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < options.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(options[i]))
                    return false;

                // Treat whitespace and compatibility-equivalent glyphs consistently.
                string normalizedOption = options[i].Normalize(NormalizationForm.FormKC).Trim().ToUpperInvariant();
                normalizedOption = string.Join(" ", normalizedOption.Split((char[])null,
                    StringSplitOptions.RemoveEmptyEntries));
                if (!normalizedOptions.Add(normalizedOption))
                    return false;
            }

            return true;
        }
    }
}
