using System;
using System.Collections.Generic;
using OrbitalEscape.Difficulty;
using UnityEngine;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [CreateAssetMenu(
        fileName = "ObstaclePattern",
        menuName = "Scriptable Objects/Generation/Obstacle Pattern")]
    public sealed class ObstaclePatternDefinition : ScriptableObject
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private PatternSlot[] _orbit1Slots;
        [SerializeField] private PatternSlot[] _orbit2Slots;
        [SerializeField] private PatternTag _tags;
        [SerializeField, Min(0.0f)] private float _baseWeight = 1.0f;
        [SerializeField, Min(0.0f)] private float _minScore;
        [SerializeField, Min(0.0f)] private float _maxScore;
        [SerializeField] private PatternDifficultyWeight[] _difficultyWeightModifiers;
        [SerializeField] private bool _allowFlip = true;

        public ObstaclePatternSnapshot CreateSnapshot(
            DifficultyType difficulty,
            PatternSlotLengthProvider lengthProvider)
        {
            return new ObstaclePatternSnapshot(
                _enabled,
                CreateSlotSnapshots(_orbit1Slots, lengthProvider),
                CreateSlotSnapshots(_orbit2Slots, lengthProvider),
                _tags,
                _baseWeight,
                _minScore,
                _maxScore,
                GetDifficultyWeight(difficulty),
                _allowFlip);
        }

        private float GetDifficultyWeight(DifficultyType difficulty)
        {
            if (_difficultyWeightModifiers == null || _difficultyWeightModifiers.Length == 0)
            {
                return 1.0f;
            }

            for (int i = 0; i < _difficultyWeightModifiers.Length; i++)
            {
                if (_difficultyWeightModifiers[i].Difficulty == difficulty)
                {
                    return Mathf.Max(0.0f, _difficultyWeightModifiers[i].WeightMultiplier);
                }
            }

            return 1.0f;
        }

        private static IReadOnlyList<PatternSlotSnapshot> CreateSlotSnapshots(
            IReadOnlyList<PatternSlot> slots,
            PatternSlotLengthProvider lengthProvider)
        {
            if (slots == null || slots.Count == 0)
            {
                return Array.Empty<PatternSlotSnapshot>();
            }

            PatternSlotSnapshot[] snapshots = new PatternSlotSnapshot[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                PatternSlot slot = slots[i];
                snapshots[i] = new PatternSlotSnapshot(slot.Type, slot.ResolveLength(lengthProvider));
            }

            return snapshots;
        }
    }

    [Serializable]
    public struct PatternSlot
    {
        public const float DefaultSpaceLength = 1.0f;
        public const float MinimumSpaceLength = 0.01f;

        [SerializeField] private PatternSlotType _type;
        [SerializeField, Min(MinimumSpaceLength)] private float _length;

        public PatternSlotType Type => _type;
        public float Length => _length > 0.0f
            ? Mathf.Max(MinimumSpaceLength, _length)
            : DefaultSpaceLength;

        public float ResolveLength(PatternSlotLengthProvider lengthProvider)
        {
            return _type == PatternSlotType.Space
                ? Length
                : lengthProvider.GetObstacleLength(_type);
        }
    }

    [Serializable]
    public struct PatternDifficultyWeight
    {
        [SerializeField] private DifficultyType _difficulty;
        [SerializeField, Min(0.0f)] private float _weightMultiplier;

        public DifficultyType Difficulty => _difficulty;
        public float WeightMultiplier => Mathf.Max(0.0f, _weightMultiplier);
    }
}
