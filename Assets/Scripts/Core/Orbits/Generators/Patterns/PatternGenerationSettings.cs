using System;
using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public sealed class PatternGenerationSettings
    {
        public IReadOnlyList<ObstaclePatternSnapshot> Patterns { get; }
        public IReadOnlyList<PatternDifficultyStageSnapshot> Stages { get; }
        public bool UseFixedSeed { get; }
        public int FixedSeed { get; }
        public int MaxSelectionAttempts { get; }

        public PatternGenerationSettings(
            IReadOnlyList<ObstaclePatternSnapshot> patterns,
            IReadOnlyList<PatternDifficultyStageSnapshot> stages,
            bool useFixedSeed,
            int fixedSeed,
            int maxSelectionAttempts)
        {
            Patterns = patterns ?? Array.Empty<ObstaclePatternSnapshot>();
            Stages = stages ?? Array.Empty<PatternDifficultyStageSnapshot>();
            UseFixedSeed = useFixedSeed;
            FixedSeed = fixedSeed;
            MaxSelectionAttempts = Math.Max(1, maxSelectionAttempts);
        }

        public PatternDifficultyStageSnapshot GetStage(int orbitIndex)
        {
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].Contains(orbitIndex))
                {
                    return Stages[i];
                }
            }

            return Stages.Count > 0
                ? Stages[^1]
                : new PatternDifficultyStageSnapshot(
                    0,
                    0,
                    0.35f,
                    0.75f,
                    PatternSlotTypeFlags.All);
        }
    }
}
