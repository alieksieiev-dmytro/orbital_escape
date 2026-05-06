using System;
using DG.Tweening;
using OrbitalEscape.Events;
using OrbitalEscape.Orbits;
using Zenject;

namespace OrbitalEscape.Movement
{
    public class JumpBehaviour : IDisposable
    {
        private const float JUMP_THROUGH_RIFT_HEIGHT = 1f;
        private const float JUMP_SPEED = 0.4f;
        
        private readonly IMovementModel _movementModel;
        private readonly LevelState _levelState;
        private readonly EventBinding<ObstacleCollidedEvent> _onObstacleCollidedEvent;
        private readonly EventBinding<OrbitsSetEvent> _onOrbitsSetEvent;

        private Orbit _currentOrbit;
        private Orbit _oppositeOrbit;
        private Sequence _jumpSequence;
        private bool _canGoThroughRift;
        
        private float Offset => _currentOrbit.Radius < _oppositeOrbit.Radius
            ? -Constants.CHARACTER_OFFSET_FROM_ORBIT
            : Constants.CHARACTER_OFFSET_FROM_ORBIT;

        [Inject]
        public JumpBehaviour(IMovementModel movementModel, LevelState levelState)
        {
            _movementModel = movementModel;
            _levelState = levelState;
            
            _onObstacleCollidedEvent = new EventBinding<ObstacleCollidedEvent>(OnObstacleCollided);
            _onOrbitsSetEvent = new EventBinding<OrbitsSetEvent>(OnOrbitsSet);
            EventBus<ObstacleCollidedEvent>.Register(_onObstacleCollidedEvent);
            EventBus<OrbitsSetEvent>.Register(_onOrbitsSetEvent);
        }

        public void Jump()
        {
            if (!TryInitializeOrbits())
            {
                return;
            }

            if (_canGoThroughRift)
                JumpThroughRift();
            else
                JumpToOppositeOrbit();
        }

        private bool TryInitializeOrbits()
        {
            if (_currentOrbit != null && _oppositeOrbit != null)
            {
                return true;
            }

            if (_levelState.Count < 2)
            {
                return false;
            }

            _currentOrbit = _levelState.GetOrbit(0);
            _oppositeOrbit = _levelState.GetOrbit(1);
            return true;
        }

        private void JumpThroughRift()
        {
            bool isJumpingOut = _currentOrbit.Index > _oppositeOrbit.Index;
            float jumpHeight = isJumpingOut ? JUMP_THROUGH_RIFT_HEIGHT : -JUMP_THROUGH_RIFT_HEIGHT;
            
            _jumpSequence?.Kill();
            _jumpSequence = DOTween.Sequence();
            _jumpSequence.Append(DOTween.To(
                () => _movementModel.Radius, 
                x => _movementModel.Radius = x,
                _currentOrbit.Radius + jumpHeight, 0.2f)
                .SetEase(Ease.Linear));
            
            _jumpSequence.Append(DOTween.To(
                () => _movementModel.Radius, 
                x => _movementModel.Radius = x,
                _currentOrbit.Radius + Offset, 0.2f)
                .SetEase(Ease.Linear));
            
            _jumpSequence.Play();
            
            EventBus<PassedThroughOrbitEvent>.Raise(new PassedThroughOrbitEvent(_currentOrbit, isJumpingOut));
        }
        
        private void JumpToOppositeOrbit()
        {
            _jumpSequence?.Kill();
            _jumpSequence = DOTween.Sequence();
            _jumpSequence.Append(DOTween.To(() => _movementModel.Radius, x => _movementModel.Radius = x, 
                _oppositeOrbit.Radius + Offset, JUMP_SPEED).SetEase(Ease.OutQuad));
            (_currentOrbit, _oppositeOrbit) = (_oppositeOrbit, _currentOrbit);
            _jumpSequence.Play();
        }
        
        private void OnObstacleCollided(ObstacleCollidedEvent @event)
        {
            if (@event.ObstacleType == ArcObjectType.Rift)
            {
                _canGoThroughRift = @event.Entered;
            }
        }
        
        private void OnOrbitsSet(OrbitsSetEvent @event)
        {
            _currentOrbit = @event.CurrentOrbit;
            _oppositeOrbit = @event.OppositeOrbit;
        }

        public void Dispose()
        {
            _jumpSequence?.Kill();
            EventBus<OrbitsSetEvent>.Deregister(_onOrbitsSetEvent);
            EventBus<ObstacleCollidedEvent>.Deregister(_onObstacleCollidedEvent);
        }
    }
}
