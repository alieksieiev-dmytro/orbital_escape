using System;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [Flags]
    public enum PatternTag
    {
        None = 0,
        Recovery = 1,
        Zigzag = 2,
        Mirror = 4,
        Alternating = 8,
        OneSideBlock = 16,
    }
}
