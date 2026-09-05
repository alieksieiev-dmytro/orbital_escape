using UnityEngine;

namespace OrbitalEscape.Audio
{
    [CreateAssetMenu(
        fileName = "RandomTimestampMusicProvider",
        menuName = "Scriptable Objects/Audio/Random Timestamp Music Provider")]
    public sealed class RandomTimestampMusicProvider : MusicProvider
    {
        private static readonly System.Random TimestampRandom = new System.Random();
        private static readonly object RandomLock = new object();

        [SerializeField] private AudioClip _track;
        [SerializeField] private int[] _startTimestampsMilliseconds;

        public override MusicPlaybackSelection GetSelection()
        {
            if (_track == null)
            {
                return default;
            }

            if (_startTimestampsMilliseconds == null || _startTimestampsMilliseconds.Length == 0)
            {
                return new MusicPlaybackSelection(_track);
            }

            int index = GetRandomIndex(_startTimestampsMilliseconds.Length);
            return new MusicPlaybackSelection(_track, _startTimestampsMilliseconds[index]);
        }

        private static int GetRandomIndex(int count)
        {
            lock (RandomLock)
            {
                return TimestampRandom.Next(count);
            }
        }
    }
}
