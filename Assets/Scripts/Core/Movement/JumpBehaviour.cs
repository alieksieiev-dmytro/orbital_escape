using DG.Tweening;
using OrbitalEscape.Events;
using OrbitalEscape.Orbits;

namespace OrbitalEscape.Movement
{
    public class JumpBehaviour
    {
        private const float JUMP_THROUGH_RIFT_HEIGHT = 1f;
        private const float JUMP_SPEED = 0.4f;
        
        private readonly IMovementModel _movementModel;

        private Orbit _currentOrbit;
        private Orbit _oppositeOrbit;
        private Sequence _jumpSequence;
        private bool _canGoThroughRift;
        
        private float Offset => _currentOrbit.Radius < _oppositeOrbit.Radius
            ? -Constants.CHARACTER_OFFSET_FROM_ORBIT
            : Constants.CHARACTER_OFFSET_FROM_ORBIT;

        public JumpBehaviour(IMovementModel movementModel, Orbit currentOrbit, Orbit oppositeOrbit)
        {
            _movementModel = movementModel;
            _currentOrbit = currentOrbit;
            _oppositeOrbit = oppositeOrbit;
            var onObstacleCollidedEvent = new EventBinding<OnObstacleCollidedEvent>(OnObstacleCollided);
            EventBus<OnObstacleCollidedEvent>.Register(onObstacleCollidedEvent);
        }
        
        public void SetOrbits(Orbit currentOrbit, Orbit oppositeOrbit)
        {
            _currentOrbit = currentOrbit;
            _oppositeOrbit = oppositeOrbit;
        }

        public void Jump()
        {
            if (_canGoThroughRift)
            {
                JumpThroughRift();
            }
            else
            {
                JumpToOppositeOrbit();
            }
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
        
        private void OnObstacleCollided(OnObstacleCollidedEvent @event)
        {
            if (@event.ObstacleType == ArcObjectType.Rift)
            {
                _canGoThroughRift = @event.Entered;
            }
        }
    }
}
