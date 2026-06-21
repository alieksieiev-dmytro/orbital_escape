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

    public sealed class WeightedPatternSelector
    {
        public bool TrySelect(
            IReadOnlyList<WeightedPatternCandidate> candidates,
            LevelRandom random,
            out PatternPlacement placement)
        {
            float totalWeight = 0.0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                totalWeight += Math.Max(0.0f, candidates[i].Weight);
            }

            if (totalWeight <= 0.0f)
            {
                placement = null;
                return false;
            }

            float pick = random.Range(0.0f, totalWeight);
            float cumulative = 0.0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += Math.Max(0.0f, candidates[i].Weight);
                if (pick <= cumulative)
                {
                    placement = candidates[i].Placement;
                    return true;
                }
            }

            placement = candidates[^1].Placement;
            return true;
        }
    }
}
