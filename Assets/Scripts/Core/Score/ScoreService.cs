using System;
using System.Collections.Generic;
using OrbitalEscape.Data;
using OrbitalEscape.Events;

namespace OrbitalEscape.Score
{
    public class ScoreService : IScoreService
    {
        private readonly IDataService _dataService;
        private readonly EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitEvent;
        private readonly List<int> _passedOrbitsIds;
        
        public float Score { get; private set; }
        public float PreviousBestScore { get; private set; }

        public event Action<float> ScoreChanged;

        public ScoreService(IDataService dataService)
        {
            _dataService = dataService;
            PreviousBestScore = _dataService.Load<ScoreData>().BestScore;
            _passedOrbitsIds = new List<int>();
            _passedThroughOrbitEvent = new EventBinding<PassedThroughOrbitEvent>(OnPassedThroughOrbit);
            EventBus<PassedThroughOrbitEvent>.Register(_passedThroughOrbitEvent);
        }

        public void Dispose()
        {
            _passedThroughOrbitEvent.Remove(OnPassedThroughOrbit);
            EventBus<PassedThroughOrbitEvent>.Deregister(_passedThroughOrbitEvent);
        }

        public void UpdateScore(float delta)
        {
            Score += delta;
            
            var scoreData = _dataService.Load<ScoreData>();
            scoreData.BestScore = Score;
            _dataService.Save(scoreData);
            
            ScoreChanged?.Invoke(Score);
        }

        public void ResetForRun()
        {
            PreviousBestScore = _dataService.Load<ScoreData>().BestScore;
            _passedOrbitsIds.Clear();
            Score = 0f;
            ScoreChanged?.Invoke(Score);
        }

        private void OnPassedThroughOrbit(PassedThroughOrbitEvent @event)
        {
            if (_passedOrbitsIds.Contains(@event.Orbit.Index))
                return;
            
            _passedOrbitsIds.Add(@event.Orbit.Index);
            UpdateScore(1);
        }
    }
}
