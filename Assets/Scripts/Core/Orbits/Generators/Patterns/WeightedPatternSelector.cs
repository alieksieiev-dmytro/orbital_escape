using System;
using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
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
