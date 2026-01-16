namespace OrbitalEscape.Events
{
    public struct PassedThroughOrbitEvent : IEvent
    {
        public readonly int OrbitIndex;
        public bool IsOuterOrbit;
        
        public PassedThroughOrbitEvent(int orbitIndex, bool isOuterOrbit)
        {
            OrbitIndex = orbitIndex;
            IsOuterOrbit = isOuterOrbit;
        }
    }
}
