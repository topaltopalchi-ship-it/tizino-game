using System;
using UnityEngine;

namespace Tiizino.Audio
{
    /// <summary>
    /// Plays authored voice clips by stable phrase ID.
    /// This is the first offline implementation; it does not synthesize TTS.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioClipSpeechService : MonoBehaviour, ITiizinoSpeechService
    {
        [Serializable]
        private sealed class VoiceLine
        {
            public string phraseId;
            public AudioClip clip;
        }

        [SerializeField] private VoiceLine[] voiceLines = Array.Empty<VoiceLine>();
        [SerializeField] private AudioSource voiceSource;

        public bool IsSpeaking => voiceSource != null && voiceSource.isPlaying;

        private void Awake()
        {
            if (voiceSource == null)
                voiceSource = GetComponent<AudioSource>();

            if (voiceSource != null)
            {
                voiceSource.playOnAwake = false;
                voiceSource.loop = false;
            }
        }

        public void Speak(string phraseId)
        {
            if (string.IsNullOrWhiteSpace(phraseId))
            {
                Debug.LogWarning("Tiizino audio: phrase ID is empty.", this);
                return;
            }

            if (voiceSource == null)
            {
                Debug.LogWarning("Tiizino audio: no AudioSource is assigned.", this);
                return;
            }

            VoiceLine line = FindLine(phraseId);
            if (line == null || line.clip == null)
            {
                Debug.LogWarning("Tiizino audio: no voice clip configured for ID '" + phraseId + "'.", this);
                return;
            }

            voiceSource.Stop();
            voiceSource.clip = line.clip;
            voiceSource.Play();
        }

        public void Stop()
        {
            if (voiceSource != null)
                voiceSource.Stop();
        }

        private VoiceLine FindLine(string phraseId)
        {
            if (voiceLines == null)
                return null;

            for (int i = 0; i < voiceLines.Length; i++)
            {
                VoiceLine line = voiceLines[i];
                if (line != null && string.Equals(line.phraseId, phraseId, StringComparison.Ordinal))
                    return line;
            }

            return null;
        }
    }
}
