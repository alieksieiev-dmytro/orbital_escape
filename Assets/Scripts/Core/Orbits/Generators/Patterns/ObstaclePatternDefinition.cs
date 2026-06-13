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
        [SerializeField] private string _id;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private PatternSlot[] _orbit1Slots;
        [SerializeField] private PatternSlot[] _orbit2Slots;
        [SerializeField] private PatternTag _tags;
        [SerializeField, Min(0.0f)] private float _cost = 1.0f;
        [SerializeField, Min(0.0f)] private float _baseWeight = 1.0f;
        [SerializeField, Min(0.0f)] private float _minScore;
        [SerializeField, Min(0.0f)] private float _maxScore;
        [SerializeField, Min(0.0f)] private float _entrySpacing;
        [SerializeField, Min(0.0f)] private float _exitSpacing;
        [SerializeField] private PatternDifficultyWeight[] _difficultyWeightModifiers;
        [SerializeField] private bool _allowFlip = true;

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

        public ObstaclePatternSnapshot CreateSnapshot(
            DifficultyType difficulty,
            PatternSlotLengthProvider lengthProvider)
        {
            return new ObstaclePatternSnapshot(
                Id,
                _enabled,
                CreateSlotSnapshots(_orbit1Slots, lengthProvider),
                CreateSlotSnapshots(_orbit2Slots, lengthProvider),
                _tags,
                _cost,
                _baseWeight,
                _minScore,
                _maxScore,
                _entrySpacing,
                _exitSpacing,
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
                PatternSlotType type = slots[i].Type;
                snapshots[i] = new PatternSlotSnapshot(type, lengthProvider.GetLength(type));
            }

            return snapshots;
        }
    }

    [Serializable]
    public struct PatternSlot
    {
        [SerializeField] private PatternSlotType _type;

        public PatternSlotType Type => _type;
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
