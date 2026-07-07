namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public readonly struct PatternPlacedObject
    {
        public Orbit Orbit { get; }
        public ArcObject ArcObject { get; }

        public PatternPlacedObject(Orbit orbit, ArcObject arcObject)
        {
            Orbit = orbit;
            ArcObject = arcObject;
        }
    }
}
