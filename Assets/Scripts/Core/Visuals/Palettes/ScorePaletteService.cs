using System;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Score;
using Zenject;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class ScorePaletteService : IPaletteService, IInitializable, IDisposable
    {
        private readonly IScoreService _scoreService;
        private readonly IDifficultyService _difficultyService;
        private readonly GamePaletteConfig _config;

        public PaletteSnapshot CurrentPalette { get; private set; }
        public event Action<PaletteSnapshot> PaletteChanged;

        [Inject]
        public ScorePaletteService(
            IScoreService scoreService,
            IDifficultyService difficultyService,
            GamePaletteConfig config)
        {
            _scoreService = scoreService;
            _difficultyService = difficultyService;
            _config = config;
            CurrentPalette = _config.Evaluate(_difficultyService.CurrentDifficulty, _scoreService.Score);
        }

        public void Initialize()
        {
            _scoreService.ScoreChanged += OnScoreChanged;
            ApplyScore(_scoreService.Score);
        }

        public void Dispose()
        {
            _scoreService.ScoreChanged -= OnScoreChanged;
        }

        private void OnScoreChanged(int score)
        {
            ApplyScore(score);
        }

        private void ApplyScore(float score)
        {
            CurrentPalette = _config.Evaluate(_difficultyService.CurrentDifficulty, score);
            PaletteChanged?.Invoke(CurrentPalette);
        }
    }
}
