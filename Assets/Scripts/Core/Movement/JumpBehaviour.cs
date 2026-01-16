using DG.Tweening;
using OrbitalEscape.Events;
using OrbitalEscape.Orbits;
using UnityEngine;

namespace OrbitalEscape.Movement
{
    public class JumpBehaviour
    {
        private const float JUMP_THROUGH_RIFT_HEIGHT = 1f;
        
        private readonly IMovementModel _movementModel;
        private readonly CollisionDetector _collisionDetector;
        private readonly Vector2 _centre;
        
        private Orbit _currentOrbit;
        private Orbit _oppositeOrbit;
        private Sequence _jumpSequence;
        private bool _isJumping;

        public JumpBehaviour(IMovementModel movementModel, CollisionDetector collisionDetector, Vector2 centre,
            Orbit currentOrbit, Orbit oppositeOrbit)
        {
            _centre = centre;
            _movementModel = movementModel;
            _collisionDetector = collisionDetector;
            _currentOrbit = currentOrbit;
            _oppositeOrbit = oppositeOrbit;
            _isJumping = false;
        }
        
        public void SetOrbits(Orbit currentOrbit, Orbit oppositeOrbit)
        {
            _currentOrbit = currentOrbit;
            _oppositeOrbit = oppositeOrbit;
        }

        public void Jump()
        {
            if (!_isJumping && _collisionDetector.IsOnRift(_currentOrbit.Rifts, _movementModel.GetPosition(_centre),
                _currentOrbit.Radius))
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
                .SetEase(Ease.OutQuad));
            
            _jumpSequence.Append(DOTween.To(
                () => _movementModel.Radius, 
                x => _movementModel.Radius = x,
                _currentOrbit.Radius, 0.2f)
                .SetEase(Ease.InQuad));
            
            _jumpSequence.onComplete += OnJumpComplete;
            _jumpSequence.Play();
            _isJumping = true;
            
            EventBus<PassedThroughOrbitEvent>.Raise(new PassedThroughOrbitEvent(_currentOrbit.Index, isJumpingOut));
        }
        
        private void JumpToOppositeOrbit()
        {
            _jumpSequence?.Kill();
            _jumpSequence = DOTween.Sequence();
            _jumpSequence.Append(DOTween.To(() => _movementModel.Radius, x => _movementModel.Radius = x, 
                _oppositeOrbit.Radius, 0.5f).SetEase(Ease.OutQuad));
            (_currentOrbit, _oppositeOrbit) = (_oppositeOrbit, _currentOrbit);
            _jumpSequence.onComplete += OnJumpComplete;
            _jumpSequence.Play();
            _isJumping = true;
        }
        
        private void OnJumpComplete() => _isJumping = false;
    }
}
