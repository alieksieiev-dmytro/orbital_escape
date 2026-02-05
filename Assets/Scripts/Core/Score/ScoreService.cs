using System;
using System.Collections.Generic;
using OrbitalEscape.Events;

namespace OrbitalEscape.Score
{
    public class ScoreService : IScoreService
    {
        private readonly EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitEvent;
        private readonly List<int> _passedOrbitsIds;
        
        public float Score { get; private set; }

        public event Action<float> ScoreChanged;

        public ScoreService()
        {
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
