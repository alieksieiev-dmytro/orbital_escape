using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [Serializable]
    public struct PatternDifficultyStage
    {
        [FormerlySerializedAs("_minScore")]
        [SerializeField, Min(0)] private int _minOrbitIndex;
        [FormerlySerializedAs("_maxScore")]
        [SerializeField, Min(0)] private int _maxOrbitIndex;
        [SerializeField, Range(0.0f, 1.0f)] private float _samePatternWeightMultiplier;
        [SerializeField, Range(0.0f, 1.0f)] private float _sameTagWeightMultiplier;
        [SerializeField] private PatternSlotTypeFlags _unlockedSlots;
        [SerializeField] private bool _ignorePatternOrbitRange;

        public static PatternDifficultyStage Default => new PatternDifficultyStage
        {
            _samePatternWeightMultiplier = 0.35f,
            _sameTagWeightMultiplier = 0.75f,
            _unlockedSlots = PatternSlotTypeFlags.All
        };

        public PatternDifficultyStageSnapshot CreateSnapshot()
        {
            return new PatternDifficultyStageSnapshot(
                _minOrbitIndex,
                _maxOrbitIndex,
                _samePatternWeightMultiplier <= 0.0f ? 0.35f : _samePatternWeightMultiplier,
                _sameTagWeightMultiplier <= 0.0f ? 0.75f : _sameTagWeightMultiplier,
                _unlockedSlots == PatternSlotTypeFlags.None ? PatternSlotTypeFlags.All : _unlockedSlots,
                _ignorePatternOrbitRange);
        }
    }
}
