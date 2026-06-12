using System.Collections.Generic;
using OrbitalEscape.Difficulty;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct LevelBuildRequest
    {
        public IReadOnlyList<Orbit> ExistingOrbits { get; }
        public int Count { get; }
        public int Seed { get; }
        public LevelBuildSettings Settings { get; }
        public DifficultyType Difficulty { get; }
        public float Score { get; }
        public float MovementSpeed { get; }

        public LevelBuildRequest(
            IReadOnlyList<Orbit> existingOrbits,
            int count,
            int seed,
            LevelBuildSettings settings,
            DifficultyType difficulty,
            float score,
            float movementSpeed)
        {
            ExistingOrbits = existingOrbits;
            Count = count;
            Seed = seed;
            Settings = settings;
            Difficulty = difficulty;
            Score = score;
            MovementSpeed = movementSpeed;
        }
    }
}
