using System;
using System.Collections.Generic;
using OrbitalEscape.Difficulty;
using UnityEngine;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [Serializable]
    public struct PatternDifficultyProfile
    {
        [SerializeField] private DifficultyType _difficulty;
        [SerializeField] private PatternDifficultyStage[] _stages;

        public DifficultyType Difficulty => _difficulty;

        public static PatternDifficultyProfile Default => new PatternDifficultyProfile
        {
            _difficulty = DifficultyType.Easy,
            _stages = new[]
            {
                PatternDifficultyStage.Default
            }
        };

        public IReadOnlyList<PatternDifficultyStageSnapshot> CreateStageSnapshots()
        {
            if (_stages == null || _stages.Length == 0)
            {
                return new[] { PatternDifficultyStage.Default.CreateSnapshot() };
            }

            PatternDifficultyStageSnapshot[] snapshots = new PatternDifficultyStageSnapshot[_stages.Length];
            for (int i = 0; i < _stages.Length; i++)
            {
                snapshots[i] = _stages[i].CreateSnapshot();
            }

            Array.Sort(snapshots, (a, b) => a.MinOrbitIndex.CompareTo(b.MinOrbitIndex));
            return snapshots;
        }
    }
}
