using UnityEngine;

namespace OrbitalEscape.Audio
{
    public readonly struct MusicPlaybackSelection
    {
        public MusicPlaybackSelection(AudioClip clip, int startTimeMilliseconds = 0)
        {
            Clip = clip;
            StartTimeMilliseconds = Mathf.Max(0, startTimeMilliseconds);
        }

        public AudioClip Clip { get; }
        public int StartTimeMilliseconds { get; }
    }
}
