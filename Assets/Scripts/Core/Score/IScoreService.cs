using System;

namespace OrbitalEscape.Score
{
    public interface IScoreService : IDisposable
    {
        float Score { get; }
        float PreviousBestScore { get; }
        
        event Action<float> ScoreChanged;
        
        void UpdateScore(float delta);
        void ResetForRun();
    }
}
