using System;
using OrbitalEscape.Difficulty;
using UnityEngine;

namespace OrbitalEscape.Audio
{
    [Serializable]
    public sealed class DifficultyMusicProfile
    {
        [SerializeField] private DifficultyType _difficulty;
        [SerializeField] private MusicProvider _provider;

        public DifficultyType Difficulty => _difficulty;

        public MusicPlaybackSelection GetSelection()
        {
            return _provider != null ? _provider.GetSelection() : default;
        }
    }
}
