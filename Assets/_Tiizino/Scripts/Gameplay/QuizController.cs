using UnityEngine;
using UnityEngine.Events;
using Tiizino.Audio;

namespace Tiizino.Gameplay
{
    /// <summary>Reusable quiz loop; connect UI events in the Unity Inspector.</summary>
    public class QuizController : MonoBehaviour
    {
        [SerializeField] private QuestionData[] questions;
        [SerializeField] private AudioClipSpeechService speechService;
        [SerializeField] private UnityEvent<string, string[]> onQuestionShown;
        [SerializeField] private UnityEvent<bool, string> onAnswerChecked;
        [SerializeField] private UnityEvent onQuizCompleted;

        private int currentIndex;
        private bool answeredCurrent;
        private bool quizCompleted;

        public int QuestionCount => questions == null ? 0 : questions.Length;
        public int CurrentIndex => currentIndex;

        private void Start()
        {
            currentIndex = 0;
            answeredCurrent = false;
            quizCompleted = false;
            ShowCurrentQuestion();
        }

        public void SubmitAnswer(int optionIndex)
        {
            if (questions == null || currentIndex < 0 || currentIndex >= questions.Length
                || answeredCurrent || quizCompleted)
                return;

            if (optionIndex < 0 || questions[currentIndex] == null
                || questions[currentIndex].options == null
                || optionIndex >= questions[currentIndex].options.Length)
            {
                Debug.LogWarning($"Tiizino: answer option index {optionIndex} is out of range.", this);
                return;
            }

            if (currentIndex < 0 || currentIndex >= questions.Length)
                return;

            QuestionData question = questions[currentIndex];
            if (question == null || !question.IsValid())
            {
                Debug.LogWarning($"Tiizino: invalid question at index {currentIndex}.", this);
                return;
            }

            bool correct = optionIndex == question.correctOptionIndex;
            answeredCurrent = true;
            onAnswerChecked?.Invoke(correct, question.explanation);
        }

        public void Continue()
        {
            if (questions == null || currentIndex < 0 || currentIndex >= questions.Length
                || quizCompleted || !answeredCurrent)
                return;

            currentIndex++;
            answeredCurrent = false;
            if (currentIndex >= questions.Length)
            {
                quizCompleted = true;
                speechService?.Stop();
                onQuizCompleted?.Invoke();
            }
            else
                ShowCurrentQuestion();
        }

        /// <summary>Replays the configured prerecorded voice line for the current question.</summary>
        public void RepeatCurrentQuestion()
        {
            SpeakCurrentQuestion();
        }

        private void SpeakCurrentQuestion()
        {
            if (speechService == null || questions == null
                || currentIndex < 0 || currentIndex >= questions.Length)
                return;

            QuestionData question = questions[currentIndex];
            if (question == null || string.IsNullOrWhiteSpace(question.audioPhraseId))
                return;

            speechService.Speak(question.audioPhraseId);
        }

        private void ShowCurrentQuestion()
        {
            if (questions == null || questions.Length == 0)
            {
                Debug.LogWarning("Tiizino: no questions configured.", this);
                return;
            }

            QuestionData question = questions[currentIndex];
            if (question == null || !question.IsValid())
            {
                Debug.LogWarning($"Tiizino: invalid question at index {currentIndex}.", this);
                return;
            }

            onQuestionShown?.Invoke(question.prompt, question.options);
            SpeakCurrentQuestion();
        }
    }
}
