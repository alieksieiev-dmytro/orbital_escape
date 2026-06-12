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
        [SerializeField] private ObstaclePatternDefinition _fallbackPattern;
        [SerializeField] private PatternDifficultyProfile[] _difficultyProfiles;
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _fixedSeed = 1337;
        [SerializeField, Min(1)] private int _maxSelectionAttempts = 64;
        [SerializeField] private bool _debugGizmosEnabled = true;

        public bool UseFixedSeed => _useFixedSeed;
        public int FixedSeed => _fixedSeed;

        public PatternGenerationSettings CreateSettings(
            DifficultyType difficulty,
            PatternSlotLengthProvider lengthProvider)
        {
            return new PatternGenerationSettings(
                CreatePatternSnapshots(difficulty, lengthProvider),
                _fallbackPattern != null ? _fallbackPattern.CreateSnapshot(difficulty, lengthProvider) : null,
                GetProfile(difficulty).CreateStageSnapshots(),
                _debugGizmosEnabled,
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

    [Serializable]
    public struct PatternDifficultyProfile
    {
        [SerializeField] private DifficultyType _difficulty;
        [SerializeField] private PatternDifficultyStage[] _stages;

        public DifficultyType Difficulty => _difficulty;

        public static PatternDifficultyProfile Default => new PatternDifficultyProfile
        {
            _difficulty = DifficultyType.Basic,
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

            Array.Sort(snapshots, (a, b) => a.MinScore.CompareTo(b.MinScore));
            return snapshots;
        }
    }

    [Serializable]
    public struct PatternDifficultyStage
    {
        [SerializeField, Min(0.0f)] private float _minScore;
        [SerializeField, Min(0.0f)] private float _maxScore;
        [SerializeField, Min(0.0f)] private float _budgetPerOrbitBand;
        [SerializeField, Range(0.0f, 1.0f)] private float _patternDensity;
        [SerializeField, Min(0.0f)] private float _maxPatternCost;
        [SerializeField, Min(0.0f)] private float _minimumSpacing;
        [SerializeField, Min(1)] private int _maxRepeatedPatternCount;
        [SerializeField, Range(0.0f, 1.0f)] private float _samePatternWeightMultiplier;
        [SerializeField, Range(0.0f, 1.0f)] private float _sameTagWeightMultiplier;
        [SerializeField] private PatternSlotTypeFlags _unlockedSlots;

        public static PatternDifficultyStage Default => new PatternDifficultyStage
        {
            _budgetPerOrbitBand = 5.0f,
            _patternDensity = 1.0f,
            _maxPatternCost = 5.0f,
            _minimumSpacing = 0.5f,
            _maxRepeatedPatternCount = 2,
            _samePatternWeightMultiplier = 0.35f,
            _sameTagWeightMultiplier = 0.75f,
            _unlockedSlots = PatternSlotTypeFlags.All
        };

        public PatternDifficultyStageSnapshot CreateSnapshot()
        {
            return new PatternDifficultyStageSnapshot(
                _minScore,
                _maxScore,
                _budgetPerOrbitBand,
                _patternDensity <= 0.0f ? 1.0f : _patternDensity,
                _maxPatternCost,
                _minimumSpacing,
                _maxRepeatedPatternCount <= 0 ? 2 : _maxRepeatedPatternCount,
                _samePatternWeightMultiplier <= 0.0f ? 0.35f : _samePatternWeightMultiplier,
                _sameTagWeightMultiplier <= 0.0f ? 0.75f : _sameTagWeightMultiplier,
                _unlockedSlots == PatternSlotTypeFlags.None ? PatternSlotTypeFlags.All : _unlockedSlots);
        }
    }
}
