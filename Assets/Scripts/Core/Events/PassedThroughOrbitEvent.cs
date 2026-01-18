using OrbitalEscape.Orbits;

namespace OrbitalEscape.Events
{
    public struct PassedThroughOrbitEvent : IEvent
    {
        public readonly Orbit Orbit;
        public readonly bool IsOuterOrbit;
        
        public PassedThroughOrbitEvent(Orbit orbit, bool isOuterOrbit)
        {
            Orbit = orbit;
            IsOuterOrbit = isOuterOrbit;
        }
    }
}
