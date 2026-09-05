using UnityEngine;

namespace OrbitalEscape.Audio
{
    [CreateAssetMenu(
        fileName = "RandomTrackMusicProvider",
        menuName = "Scriptable Objects/Audio/Random Track Music Provider")]
    public sealed class RandomTrackMusicProvider : MusicProvider
    {
        [SerializeField] private AudioClip[] _tracks;

        public override MusicPlaybackSelection GetSelection()
        {
            if (_tracks == null || _tracks.Length == 0)
            {
                return default;
            }

            int availableTrackCount = 0;
            for (int i = 0; i < _tracks.Length; i++)
            {
                if (_tracks[i] != null)
                {
                    availableTrackCount++;
                }
            }

            if (availableTrackCount == 0)
            {
                return default;
            }

            int selectedTrackIndex = Random.Range(0, availableTrackCount);
            for (int i = 0; i < _tracks.Length; i++)
            {
                AudioClip track = _tracks[i];
                if (track == null)
                {
                    continue;
                }

                if (selectedTrackIndex == 0)
                {
                    return new MusicPlaybackSelection(track);
                }

                selectedTrackIndex--;
            }

            return default;
        }
    }
}
