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
        [SerializeField] private PatternDifficultyStage[] _stages;
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _fixedSeed = 1337;
        [SerializeField, Min(1)] private int _maxSelectionAttempts = 64;
        [SerializeField] private bool _randomizeBandPhase = true;
        [SerializeField, Min(0.0f)]
        [Tooltip("Empty world distance between patterns when obstacles switch physical orbit lanes at their boundary.")]
        private float _safePadding = 2.0f;

        public bool UseFixedSeed => _useFixedSeed;
        public int FixedSeed => _fixedSeed;
        public bool RandomizeBandPhase => _randomizeBandPhase;
        public IReadOnlyList<ObstaclePatternDefinition> Patterns => _patterns ?? Array.Empty<ObstaclePatternDefinition>();

        public PatternGenerationSettings CreateSettings(
            DifficultyType difficulty,
            PatternSlotLengthProvider lengthProvider)
        {
            return new PatternGenerationSettings(
                CreatePatternSnapshots(difficulty, lengthProvider),
                CreateStageSnapshots(),
                _useFixedSeed,
                _fixedSeed,
                _maxSelectionAttempts,
                _safePadding);
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

        private IReadOnlyList<PatternDifficultyStageSnapshot> CreateStageSnapshots()
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
