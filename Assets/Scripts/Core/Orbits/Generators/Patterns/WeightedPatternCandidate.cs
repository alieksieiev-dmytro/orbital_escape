namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public readonly struct WeightedPatternCandidate
    {
        public PatternPlacement Placement { get; }
        public float Weight { get; }

        public WeightedPatternCandidate(PatternPlacement placement, float weight)
        {
            Placement = placement;
            Weight = weight;
        }
    }
}
