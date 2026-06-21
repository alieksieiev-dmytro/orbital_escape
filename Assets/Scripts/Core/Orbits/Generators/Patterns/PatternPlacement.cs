using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public sealed class PatternPlacement
    {
        private readonly List<PatternPlacedObject> _objects;

        public ObstaclePatternSnapshot Pattern { get; }
        public bool Flipped { get; }
        public Orbit LowerOrbit { get; }
        public Orbit UpperOrbit { get; }
        public float StartWorld { get; }
        public float StartAngleDegree { get; }
        public float EndAngleDegree { get; }
        public IReadOnlyList<PatternPlacedObject> Objects => _objects;

        public PatternPlacement(
            ObstaclePatternSnapshot pattern,
            bool flipped,
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float startWorld,
            float startAngleDegree,
            float endAngleDegree,
            List<PatternPlacedObject> objects)
        {
            Pattern = pattern;
            Flipped = flipped;
            LowerOrbit = lowerOrbit;
            UpperOrbit = upperOrbit;
            StartWorld = startWorld;
            StartAngleDegree = startAngleDegree;
            EndAngleDegree = endAngleDegree;
            _objects = objects ?? new List<PatternPlacedObject>();
        }
    }

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
