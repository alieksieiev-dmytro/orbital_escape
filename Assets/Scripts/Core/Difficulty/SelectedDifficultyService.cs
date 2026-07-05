namespace OrbitalEscape.Difficulty
{
    public sealed class SelectedDifficultyService : ISelectedDifficultyService
    {
        public DifficultyType CurrentDifficulty { get; private set; } = DifficultyType.Easy;

        public void SetDifficulty(DifficultyType difficulty)
        {
            CurrentDifficulty = difficulty;
        }
    }
}
