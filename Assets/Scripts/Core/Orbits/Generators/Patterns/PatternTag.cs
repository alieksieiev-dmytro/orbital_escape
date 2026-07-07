using System;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [Flags]
    public enum PatternTag
    {
        None = 0,
        Recovery = 1,
        Complement = 2,
        Mirror = 4,
        Alternating = 8,
    }
}
