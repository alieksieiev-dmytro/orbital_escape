namespace OrbitalEscape.Difficulty
{
    public sealed class BasicDifficultyService : IDifficultyService
    {
        public DifficultyType CurrentDifficulty => DifficultyType.Basic;
    }
}
