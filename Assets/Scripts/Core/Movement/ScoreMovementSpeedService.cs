using System;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Score;
using Zenject;

namespace OrbitalEscape.Movement
{
    public sealed class ScoreMovementSpeedService : IMovementSpeedService, IInitializable, IDisposable
    {
        private readonly IScoreService _scoreService;
        private readonly IDifficultyService _difficultyService;
        private readonly MovementSpeedConfig _config;

        public float CurrentLinearSpeed { get; private set; }

        [Inject]
        public ScoreMovementSpeedService(
            IScoreService scoreService,
            IDifficultyService difficultyService,
            MovementSpeedConfig config)
        {
            _scoreService = scoreService;
            _difficultyService = difficultyService;
            _config = config;
            ApplyScore(_scoreService.Score);
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

        private void OnScoreChanged(float score)
        {
            ApplyScore(score);
        }

        private void ApplyScore(float score)
        {
            CurrentLinearSpeed = _config.Evaluate(_difficultyService.CurrentDifficulty, score);
        }
    }
}
