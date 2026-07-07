using System;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public readonly struct PatternSlotSnapshot
    {
        public PatternSlotType Type { get; }
        public float Length { get; }

        public PatternSlotSnapshot(PatternSlotType type, float length)
        {
            Type = type;
            Length = Math.Max(0.0f, length);
        }

        public bool IsObstacle => Type != PatternSlotType.Space;
    }
}
