using System;

namespace OrbitalEscape.Score
{
    public interface IScoreService : IDisposable
    {
        int Score { get; }
        int BestScore { get; }
        int PreviousBestScore { get; }
        
        event Action<int> ScoreChanged;
        
        void UpdateScore(int delta);
        void ResetForRun();
    }
}
