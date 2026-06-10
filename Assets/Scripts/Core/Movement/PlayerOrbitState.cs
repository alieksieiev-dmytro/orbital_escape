using System;
using OrbitalEscape.Events;
using OrbitalEscape.Orbits;
using Zenject;

namespace OrbitalEscape.Movement
{
    public sealed class PlayerOrbitState : IInitializable, IDisposable
    {
        private readonly LevelState _levelState;
        private EventBinding<OrbitsSetEvent> _orbitsSetBinding;

        public Orbit CurrentOrbit { get; private set; }
        public Orbit OppositeOrbit { get; private set; }

        [Inject]
        public PlayerOrbitState(LevelState levelState)
        {
            _levelState = levelState;
        }

        public void Initialize()
        {
            _orbitsSetBinding = new EventBinding<OrbitsSetEvent>(OnOrbitsSet);
            EventBus<OrbitsSetEvent>.Register(_orbitsSetBinding);
        }

        public bool TryInitialize()
        {
            if (CurrentOrbit != null && OppositeOrbit != null)
            {
                return true;
            }

            if (_levelState.Count < 2)
            {
                return false;
            }

            SetCurrentAndOpposite(_levelState.GetOrbit(0), _levelState.GetOrbit(1));
            return true;
        }

        public void Swap()
        {
            (CurrentOrbit, OppositeOrbit) = (OppositeOrbit, CurrentOrbit);
        }

        public void SetCurrentAndOpposite(Orbit currentOrbit, Orbit oppositeOrbit)
        {
            CurrentOrbit = currentOrbit;
            OppositeOrbit = oppositeOrbit;
        }

        public float GetMovementRadius(Orbit orbit, Orbit oppositeOrbit)
        {
            float offset = orbit.Radius < oppositeOrbit.Radius
                ? Constants.CHARACTER_OFFSET_FROM_ORBIT
                : -Constants.CHARACTER_OFFSET_FROM_ORBIT;

            return orbit.Radius + offset;
        }

        public float GetRiftExitRadius()
        {
            float offset = CurrentOrbit.Radius < OppositeOrbit.Radius
                ? -Constants.CHARACTER_OFFSET_FROM_ORBIT
                : Constants.CHARACTER_OFFSET_FROM_ORBIT;

            return CurrentOrbit.Radius + offset;
        }

        public void Dispose()
        {
            if (_orbitsSetBinding != null)
            {
                EventBus<OrbitsSetEvent>.Deregister(_orbitsSetBinding);
            }
        }

        private void OnOrbitsSet(OrbitsSetEvent @event)
        {
            SetCurrentAndOpposite(@event.CurrentOrbit, @event.OppositeOrbit);
        }
    }
}
