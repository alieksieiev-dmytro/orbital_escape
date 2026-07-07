#if UNITY_EDITOR
using OrbitalEscape.Orbits.Generators.Patterns;

namespace OrbitalEscape.Editor
{
    internal sealed class PatternProbabilityRow
    {
        public string Name { get; }
        public ObstaclePatternSnapshot Pattern { get; }
        public int Orientations { get; }
        public float BaseEffectiveWeight { get; }
        public float AggregateWeight { get; }
        public string UnavailableReason { get; }
        public bool Available => string.IsNullOrEmpty(UnavailableReason);

        public PatternProbabilityRow(
            string name,
            ObstaclePatternSnapshot pattern,
            int orientations,
            float baseEffectiveWeight,
            float aggregateWeight,
            string unavailableReason)
        {
            Name = name;
            Pattern = pattern;
            Orientations = orientations;
            BaseEffectiveWeight = baseEffectiveWeight;
            AggregateWeight = aggregateWeight;
            UnavailableReason = unavailableReason;
        }
    }
}
#endif
