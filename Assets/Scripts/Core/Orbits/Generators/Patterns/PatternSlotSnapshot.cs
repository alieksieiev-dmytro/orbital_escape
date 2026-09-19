using System;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public readonly struct PatternSlotSnapshot
    {
        public PatternSlotType Type { get; }
        public float Length { get; }
        public float Height { get; }
        public float MinHeight { get; }
        public float MaxHeight { get; }

        public PatternSlotSnapshot(
            PatternSlotType type,
            float length,
            float height,
            float minHeight,
            float maxHeight)
        {
            Type = type;
            Length = Math.Max(0.0f, length);
            Height = type != PatternSlotType.Spike
                ? 0.0f
                : height > 0.0f
                    ? Math.Max(PatternSlot.MinimumSpikeHeight, height)
                    : PatternSlot.DefaultSpikeHeight;
            MinHeight = type != PatternSlotType.RandomizedSpike
                ? 0.0f
                : minHeight > 0.0f
                    ? Math.Max(PatternSlot.MinimumSpikeHeight, minHeight)
                    : PatternSlot.DefaultSpikeHeight;
            MaxHeight = type != PatternSlotType.RandomizedSpike
                ? 0.0f
                : Math.Max(
                    MinHeight,
                    maxHeight > 0.0f ? maxHeight : PatternSlot.DefaultRandomizedSpikeMaxHeight);
        }

        public bool IsObstacle => Type != PatternSlotType.Space;
    }
}
