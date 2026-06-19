using System;
using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public enum PatternSlotType
    {
        Space = 0,
        Spike = 1,
        ImpulseTrap = 2,
        GravityWell = 3,
        Rift = 4,
        HalfSpace = 5
    }

    [Flags]
    public enum PatternSlotTypeFlags
    {
        None = 0,
        Space = 1 << 0,
        Spike = 1 << 1,
        ImpulseTrap = 1 << 2,
        GravityWell = 1 << 3,
        Rift = 1 << 4,
        All = Space | Spike | ImpulseTrap | GravityWell | Rift
    }

    [Flags]
    public enum PatternTag
    {
        None = 0,
        Recovery = 1 << 0,
        Spike = 1 << 1,
        Trap = 1 << 2,
        Gravity = 1 << 3,
        Rift = 1 << 4,
        Compound = 1 << 5,
        Zigzag = 1 << 6,
    }

    public readonly struct PatternSlotSnapshot
    {
        public PatternSlotType Type { get; }
        public float Length { get; }

        public PatternSlotSnapshot(PatternSlotType type, float length)
        {
            Type = type;
            Length = Math.Max(0.0f, length);
        }

        public bool IsObstacle => Type != PatternSlotType.Space &&
                                  Type != PatternSlotType.HalfSpace;
    }

    public sealed class ObstaclePatternSnapshot
    {
        public bool Enabled { get; }
        public IReadOnlyList<PatternSlotSnapshot> Orbit1Slots { get; }
        public IReadOnlyList<PatternSlotSnapshot> Orbit2Slots { get; }
        public PatternTag Tags { get; }
        public float BaseWeight { get; }
        public float MinScore { get; }
        public float MaxScore { get; }
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
            float minScore,
            float maxScore,
            float difficultyWeightMultiplier,
            bool allowFlip)
        {
            Enabled = enabled;
            Orbit1Slots = orbit1Slots ?? Array.Empty<PatternSlotSnapshot>();
            Orbit2Slots = orbit2Slots ?? Array.Empty<PatternSlotSnapshot>();
            Tags = tags;
            BaseWeight = Math.Max(0.0f, baseWeight);
            MinScore = Math.Max(0.0f, minScore);
            MaxScore = maxScore <= 0.0f ? float.PositiveInfinity : Math.Max(minScore, maxScore);
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

        private static bool ContainsLockedSlot(IReadOnlyList<PatternSlotSnapshot> slots, PatternSlotTypeFlags unlockedSlots)
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

    public readonly struct PatternDifficultyStageSnapshot
    {
        public float MinScore { get; }
        public float MaxScore { get; }
        public float SamePatternWeightMultiplier { get; }
        public float SameTagWeightMultiplier { get; }
        public PatternSlotTypeFlags UnlockedSlots { get; }

        public PatternDifficultyStageSnapshot(
            float minScore,
            float maxScore,
            float samePatternWeightMultiplier,
            float sameTagWeightMultiplier,
            PatternSlotTypeFlags unlockedSlots)
        {
            MinScore = Math.Max(0.0f, minScore);
            MaxScore = maxScore <= 0.0f ? float.PositiveInfinity : Math.Max(minScore, maxScore);
            SamePatternWeightMultiplier = Math.Min(Math.Max(samePatternWeightMultiplier, 0.0f), 1.0f);
            SameTagWeightMultiplier = Math.Min(Math.Max(sameTagWeightMultiplier, 0.0f), 1.0f);
            UnlockedSlots = unlockedSlots == PatternSlotTypeFlags.None
                ? PatternSlotTypeFlags.Space
                : unlockedSlots;
        }

        public bool Contains(float score)
        {
            return score >= MinScore && score <= MaxScore;
        }
    }

    public sealed class PatternGenerationSettings
    {
        public IReadOnlyList<ObstaclePatternSnapshot> Patterns { get; }
        public IReadOnlyList<PatternDifficultyStageSnapshot> Stages { get; }
        public bool UseFixedSeed { get; }
        public int FixedSeed { get; }
        public int MaxSelectionAttempts { get; }

        public PatternGenerationSettings(
            IReadOnlyList<ObstaclePatternSnapshot> patterns,
            IReadOnlyList<PatternDifficultyStageSnapshot> stages,
            bool useFixedSeed,
            int fixedSeed,
            int maxSelectionAttempts)
        {
            Patterns = patterns ?? Array.Empty<ObstaclePatternSnapshot>();
            Stages = stages ?? Array.Empty<PatternDifficultyStageSnapshot>();
            UseFixedSeed = useFixedSeed;
            FixedSeed = fixedSeed;
            MaxSelectionAttempts = Math.Max(1, maxSelectionAttempts);
        }

        public PatternDifficultyStageSnapshot GetStage(float score)
        {
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].Contains(score))
                {
                    return Stages[i];
                }
            }

            return Stages.Count > 0
                ? Stages[^1]
                : new PatternDifficultyStageSnapshot(
                    0.0f,
                    0.0f,
                    0.35f,
                    0.75f,
                    PatternSlotTypeFlags.All);
        }
    }

    public static class PatternSlotTypeFlagsExtensions
    {
        public static bool Contains(this PatternSlotTypeFlags flags, PatternSlotType type)
        {
            PatternSlotTypeFlags target = type switch
            {
                PatternSlotType.Space => PatternSlotTypeFlags.Space,
                PatternSlotType.HalfSpace => PatternSlotTypeFlags.Space,
                PatternSlotType.Spike => PatternSlotTypeFlags.Spike,
                PatternSlotType.ImpulseTrap => PatternSlotTypeFlags.ImpulseTrap,
                PatternSlotType.GravityWell => PatternSlotTypeFlags.GravityWell,
                PatternSlotType.Rift => PatternSlotTypeFlags.Rift,
                _ => PatternSlotTypeFlags.None
            };

            return (flags & target) == target;
        }
    }

    public readonly struct PatternSlotLengthProvider
    {
        public const float HalfSpaceLength = 0.5f;

        private readonly float _spaceLength;
        private readonly float _spikeLength;
        private readonly float _impulseTrapLength;
        private readonly float _gravityWellLength;
        private readonly float _riftLength;

        public PatternSlotLengthProvider(
            float spaceLength,
            float spikeLength,
            float impulseTrapLength,
            float gravityWellLength,
            float riftLength)
        {
            _spaceLength = Math.Max(0.01f, spaceLength);
            _spikeLength = Math.Max(0.01f, spikeLength);
            _impulseTrapLength = Math.Max(0.01f, impulseTrapLength);
            _gravityWellLength = Math.Max(0.01f, gravityWellLength);
            _riftLength = Math.Max(0.01f, riftLength);
        }

        public float GetLength(PatternSlotType type)
        {
            return type switch
            {
                PatternSlotType.Space => _spaceLength,
                PatternSlotType.HalfSpace => HalfSpaceLength,
                PatternSlotType.Spike => _spikeLength,
                PatternSlotType.ImpulseTrap => _impulseTrapLength,
                PatternSlotType.GravityWell => _gravityWellLength,
                PatternSlotType.Rift => _riftLength,
                _ => _spaceLength
            };
        }
    }
}
