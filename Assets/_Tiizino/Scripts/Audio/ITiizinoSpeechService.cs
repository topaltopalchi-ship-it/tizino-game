namespace Tiizino.Audio
{
    /// <summary>
    /// Small abstraction for spoken guidance. Gameplay refers to phrase IDs,
    /// not a specific voice engine, so an offline TTS backend can be added later.
    /// </summary>
    public interface ITiizinoSpeechService
    {
        bool IsSpeaking { get; }
        void Speak(string phraseId);
        void Stop();
    }
}
