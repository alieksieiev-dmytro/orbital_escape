namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public static class PatternSlotTypeFlagsExtensions
    {
        public static bool Contains(this PatternSlotTypeFlags flags, PatternSlotType type)
        {
            PatternSlotTypeFlags target = type switch
            {
                PatternSlotType.Space => PatternSlotTypeFlags.Space,
                PatternSlotType.Spike => PatternSlotTypeFlags.Spike,
                PatternSlotType.ImpulseTrap => PatternSlotTypeFlags.ImpulseTrap,
                PatternSlotType.GravityWell => PatternSlotTypeFlags.GravityWell,
                PatternSlotType.Rift => PatternSlotTypeFlags.Rift,
                PatternSlotType.MiniSpike => PatternSlotTypeFlags.MiniSpike,
                PatternSlotType.LaserGate => PatternSlotTypeFlags.LaserGate,
                _ => PatternSlotTypeFlags.None
            };

            return (flags & target) == target;
        }
    }
}
