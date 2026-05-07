using System;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class LevelRandom
    {
        private readonly Random _random;

        public LevelRandom(int seed)
        {
            _random = new Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }

        public float Range(float minInclusive, float maxExclusive)
        {
            return minInclusive + (float)_random.NextDouble() * (maxExclusive - minInclusive);
        }
    }
}
