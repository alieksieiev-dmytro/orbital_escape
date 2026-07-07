using System;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [Flags]
    public enum PatternSlotTypeFlags
    {
        None = 0,
        Space = 1 << 0,
        Spike = 1 << 1,
        ImpulseTrap = 1 << 2,
        GravityWell = 1 << 3,
        Rift = 1 << 4,
        MiniSpike = 1 << 5,
        LaserGate = 1 << 6,
        All = Space | Spike | ImpulseTrap | GravityWell | Rift | MiniSpike | LaserGate
    }
}
