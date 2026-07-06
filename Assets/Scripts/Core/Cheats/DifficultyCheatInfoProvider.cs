using OrbitalEscape.Difficulty;
using Zenject;

namespace OrbitalEscape.Cheats
{
    public sealed class DifficultyCheatInfoProvider : ICheatInfoProvider
    {
        private readonly IDifficultyService _difficultyService;

        public int Order => 10;

        [Inject]
        public DifficultyCheatInfoProvider(IDifficultyService difficultyService)
        {
            _difficultyService = difficultyService;
        }

        public string GetLine()
        {
            return $"Difficulty: {_difficultyService.CurrentDifficulty}";
        }
    }
}
