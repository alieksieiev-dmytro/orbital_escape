using System;

namespace OrbitalEscape.Score
{
    public interface IScoreService : IDisposable
    {
        float Score { get; }
        
        event Action<float> ScoreChanged;
        
        void UpdateScore(float delta);
    }
}
