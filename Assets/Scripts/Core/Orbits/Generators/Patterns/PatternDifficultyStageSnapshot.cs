using System;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public readonly struct PatternDifficultyStageSnapshot
    {
        public int MinOrbitIndex { get; }
        public int MaxOrbitIndex { get; }
        public float SamePatternWeightMultiplier { get; }
        public float SameTagWeightMultiplier { get; }
        public PatternSlotTypeFlags UnlockedSlots { get; }

        public PatternDifficultyStageSnapshot(
            int minOrbitIndex,
            int maxOrbitIndex,
            float samePatternWeightMultiplier,
            float sameTagWeightMultiplier,
            PatternSlotTypeFlags unlockedSlots)
        {
            MinOrbitIndex = Math.Max(0, minOrbitIndex);
            MaxOrbitIndex = maxOrbitIndex <= 0 ? int.MaxValue : Math.Max(MinOrbitIndex, maxOrbitIndex);
            SamePatternWeightMultiplier = Math.Min(Math.Max(samePatternWeightMultiplier, 0.0f), 1.0f);
            SameTagWeightMultiplier = Math.Min(Math.Max(sameTagWeightMultiplier, 0.0f), 1.0f);
            UnlockedSlots = unlockedSlots == PatternSlotTypeFlags.None
                ? PatternSlotTypeFlags.Space
                : unlockedSlots;
        }

        public bool Contains(int orbitIndex)
        {
            return orbitIndex >= MinOrbitIndex && orbitIndex <= MaxOrbitIndex;
        }
    }
}
