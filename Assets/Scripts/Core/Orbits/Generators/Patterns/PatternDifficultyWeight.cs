using System;
using OrbitalEscape.Difficulty;
using UnityEngine;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [Serializable]
    public struct PatternDifficultyWeight
    {
        [SerializeField] private DifficultyType _difficulty;
        [SerializeField, Min(0.0f)] private float _weightMultiplier;

        public DifficultyType Difficulty => _difficulty;
        public float WeightMultiplier => Mathf.Max(0.0f, _weightMultiplier);
    }
}
