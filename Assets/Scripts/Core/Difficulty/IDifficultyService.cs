namespace OrbitalEscape.Difficulty
{
    public interface IDifficultyService
    {
        DifficultyType CurrentDifficulty { get; }
    }
}
