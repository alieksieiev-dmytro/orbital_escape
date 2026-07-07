using System;
using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public static class PatternWeightCalculator
    {
        public static float GetEffectiveWeight(
            ObstaclePatternSnapshot pattern,
            PatternDifficultyStageSnapshot stage,
            IReadOnlyList<ObstaclePatternSnapshot> history)
        {
            int samePatternStreak = 0;
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(history[i], pattern))
                {
                    break;
                }

                samePatternStreak++;
            }

            float repetitionModifier = samePatternStreak > 0
                ? (float)Math.Pow(stage.SamePatternWeightMultiplier, samePatternStreak)
                : 1.0f;

            if (history.Count > 0 &&
                (history[^1].Tags & pattern.Tags) != PatternTag.None &&
                !ReferenceEquals(history[^1], pattern))
            {
                repetitionModifier *= stage.SameTagWeightMultiplier;
            }

            return pattern.BaseWeight *
                   pattern.DifficultyWeightMultiplier *
                   repetitionModifier;
        }
    }
}
