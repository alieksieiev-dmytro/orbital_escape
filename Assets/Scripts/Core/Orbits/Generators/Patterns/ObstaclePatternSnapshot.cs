using System;
using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public sealed class ObstaclePatternSnapshot
    {
        public bool Enabled { get; }
        public IReadOnlyList<PatternSlotSnapshot> Orbit1Slots { get; }
        public IReadOnlyList<PatternSlotSnapshot> Orbit2Slots { get; }
        public PatternTag Tags { get; }
        public float BaseWeight { get; }
        public int MinOrbitIndex { get; }
        public int MaxOrbitIndex { get; }
        public float DifficultyWeightMultiplier { get; }
        public bool AllowFlip { get; }
        public float Orbit1Length { get; }
        public float Orbit2Length { get; }
        public int Orbit1RiftCount { get; }
        public int Orbit2RiftCount { get; }
        public float Length { get; }

        public ObstaclePatternSnapshot(
            bool enabled,
            IReadOnlyList<PatternSlotSnapshot> orbit1Slots,
            IReadOnlyList<PatternSlotSnapshot> orbit2Slots,
            PatternTag tags,
            float baseWeight,
            int minOrbitIndex,
            int maxOrbitIndex,
            float difficultyWeightMultiplier,
            bool allowFlip)
        {
            Enabled = enabled;
            Orbit1Slots = orbit1Slots ?? Array.Empty<PatternSlotSnapshot>();
            Orbit2Slots = orbit2Slots ?? Array.Empty<PatternSlotSnapshot>();
            Tags = tags;
            BaseWeight = Math.Max(0.0f, baseWeight);
            MinOrbitIndex = Math.Max(0, minOrbitIndex);
            MaxOrbitIndex = maxOrbitIndex <= 0 ? int.MaxValue : Math.Max(MinOrbitIndex, maxOrbitIndex);
            DifficultyWeightMultiplier = Math.Max(0.0f, difficultyWeightMultiplier);
            AllowFlip = allowFlip;
            Orbit1Length = SumLength(Orbit1Slots);
            Orbit2Length = SumLength(Orbit2Slots);
            Orbit1RiftCount = CountRifts(Orbit1Slots);
            Orbit2RiftCount = CountRifts(Orbit2Slots);
            Length = Math.Max(Orbit1Length, Orbit2Length);
        }

        public bool ContainsLockedSlot(PatternSlotTypeFlags unlockedSlots)
        {
            return ContainsLockedSlot(Orbit1Slots, unlockedSlots) ||
                   ContainsLockedSlot(Orbit2Slots, unlockedSlots);
        }

        public bool HasObstacleOnOrbit1()
        {
            return HasObstacle(Orbit1Slots);
        }

        public bool HasObstacleOnOrbit2()
        {
            return HasObstacle(Orbit2Slots);
        }

        public int GetLowerLaneRiftCount(bool flipped)
        {
            return flipped ? Orbit2RiftCount : Orbit1RiftCount;
        }

        public int GetUpperLaneRiftCount(bool flipped)
        {
            return flipped ? Orbit1RiftCount : Orbit2RiftCount;
        }

        private static float SumLength(IReadOnlyList<PatternSlotSnapshot> slots)
        {
            float length = 0.0f;
            for (int i = 0; i < slots.Count; i++)
            {
                length += slots[i].Length;
            }

            return length;
        }

        private static int CountRifts(IReadOnlyList<PatternSlotSnapshot> slots)
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Type == PatternSlotType.Rift)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool ContainsLockedSlot(
            IReadOnlyList<PatternSlotSnapshot> slots,
            PatternSlotTypeFlags unlockedSlots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (!unlockedSlots.Contains(slots[i].Type))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasObstacle(IReadOnlyList<PatternSlotSnapshot> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsObstacle)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
