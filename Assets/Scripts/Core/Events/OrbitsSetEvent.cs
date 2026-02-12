using OrbitalEscape.Orbits;

namespace OrbitalEscape.Events
{
    public readonly struct OrbitsSetEvent : IEvent
    {
        public readonly Orbit CurrentOrbit;
        public readonly Orbit OppositeOrbit;
        
        public OrbitsSetEvent(Orbit currentOrbit, Orbit oppositeOrbit)
        {
            CurrentOrbit = currentOrbit;
            OppositeOrbit = oppositeOrbit;
        }
    }
}
