using System;
using System.Collections.Generic;
using OrbitalEscape.Difficulty;
using UnityEngine;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [CreateAssetMenu(
        fileName = "ObstaclePatternGenerationConfig",
        menuName = "Scriptable Objects/Configs/Obstacle Pattern Generation Config")]
    public sealed class ObstaclePatternGenerationConfig : ScriptableObject
    {
        [SerializeField] private ObstaclePatternDefinition[] _patterns;
        [SerializeField] private PatternDifficultyProfile[] _difficultyProfiles;
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _fixedSeed = 1337;
        [SerializeField, Min(1)] private int _maxSelectionAttempts = 64;
        [SerializeField] private bool _randomizeBandPhase = true;

        public bool UseFixedSeed => _useFixedSeed;
        public int FixedSeed => _fixedSeed;
        public bool RandomizeBandPhase => _randomizeBandPhase;

        public PatternGenerationSettings CreateSettings(
            DifficultyType difficulty,
            PatternSlotLengthProvider lengthProvider)
        {
            return new PatternGenerationSettings(
                CreatePatternSnapshots(difficulty, lengthProvider),
                GetProfile(difficulty).CreateStageSnapshots(),
                _useFixedSeed,
                _fixedSeed,
                _maxSelectionAttempts);
        }

        private IReadOnlyList<ObstaclePatternSnapshot> CreatePatternSnapshots(
            DifficultyType difficulty,
            PatternSlotLengthProvider lengthProvider)
        {
            if (_patterns == null || _patterns.Length == 0)
            {
                return Array.Empty<ObstaclePatternSnapshot>();
            }

            List<ObstaclePatternSnapshot> snapshots = new List<ObstaclePatternSnapshot>(_patterns.Length);
            for (int i = 0; i < _patterns.Length; i++)
            {
                if (_patterns[i] == null)
                {
                    continue;
                }

                snapshots.Add(_patterns[i].CreateSnapshot(difficulty, lengthProvider));
            }

            return snapshots;
        }

        private PatternDifficultyProfile GetProfile(DifficultyType difficulty)
        {
            if (_difficultyProfiles != null)
            {
                for (int i = 0; i < _difficultyProfiles.Length; i++)
                {
                    if (_difficultyProfiles[i].Difficulty == difficulty)
                    {
                        return _difficultyProfiles[i];
                    }
                }
            }

            return PatternDifficultyProfile.Default;
        }
    }
}
