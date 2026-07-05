namespace OrbitalEscape.Difficulty
{
    public interface ISelectedDifficultyService : IDifficultyService
    {
        void SetDifficulty(DifficultyType difficulty);
    }
}
