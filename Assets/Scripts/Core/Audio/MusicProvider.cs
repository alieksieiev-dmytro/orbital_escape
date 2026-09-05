using UnityEngine;

namespace OrbitalEscape.Audio
{
    public abstract class MusicProvider : ScriptableObject
    {
        public abstract MusicPlaybackSelection GetSelection();
    }
}
