using System;
using DG.Tweening;
using OrbitalEscape.Events;
using OrbitalEscape.Orbits;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    public class JumpBehaviour : IDisposable
    {
        private const float JUMP_THROUGH_RIFT_HEIGHT = 1f;
        private const float JUMP_SPEED = 0.4f;
        
        private readonly IMovementModel _movementModel;
        private readonly PlayerOrbitState _orbitState;
        private readonly IGravityWellInfluenceService _gravityWellInfluenceService;
        private readonly IOrbitJumpTransitionService _jumpTransitionService;
        private readonly EventBinding<ObstacleCollidedEvent> _onObstacleCollidedEvent;

        private bool _canGoThroughRift;
        private float _jumpInputUnlockTime;

        [Inject]
        public JumpBehaviour(
            IMovementModel movementModel,
            PlayerOrbitState orbitState,
            IGravityWellInfluenceService gravityWellInfluenceService,
            IOrbitJumpTransitionService jumpTransitionService)
        {
            _movementModel = movementModel;
            _orbitState = orbitState;
            _gravityWellInfluenceService = gravityWellInfluenceService;
            _jumpTransitionService = jumpTransitionService;
            
            _onObstacleCollidedEvent = new EventBinding<ObstacleCollidedEvent>(OnObstacleCollided);
            EventBus<ObstacleCollidedEvent>.Register(_onObstacleCollidedEvent);
        }

        public void Jump()
        {
            if (Time.time < _jumpInputUnlockTime)
            {
                return;
            }

            JumpInternal();
        }

        public void JumpFromImpulseTrap(float inputLockDuration)
        {
            if (!JumpInternal())
            {
                return;
            }

            _jumpInputUnlockTime = Mathf.Max(
                _jumpInputUnlockTime,
                Time.time + Mathf.Max(0.0f, inputLockDuration));
        }

        private bool JumpInternal()
        {
            if (!_orbitState.TryInitialize())
            {
                return false;
            }

            if (_canGoThroughRift)
            {
                _gravityWellInfluenceService.SuppressActiveUntilExit();
                JumpThroughRift();
                EventBus<PlayerJumpedEvent>.Raise(new PlayerJumpedEvent());
                return true;
            }

            if (_gravityWellInfluenceService.TryGetActiveZone(out GravityWellZone zone))
            {
                JumpPartiallyFromGravityWell(zone);
                EventBus<PlayerJumpedEvent>.Raise(new PlayerJumpedEvent());
                return true;
            }

            JumpToOppositeOrbit();
            EventBus<PlayerJumpedEvent>.Raise(new PlayerJumpedEvent());
            return true;
        }

        private void JumpThroughRift()
        {
            Orbit currentOrbit = _orbitState.CurrentOrbit;
            bool isJumpingOut = currentOrbit.Index > _orbitState.OppositeOrbit.Index;
            float jumpHeight = isJumpingOut ? JUMP_THROUGH_RIFT_HEIGHT : -JUMP_THROUGH_RIFT_HEIGHT;

            _jumpTransitionService.Play(new[]
            {
                new OrbitJumpStep(currentOrbit.Radius + jumpHeight, 0.2f, Ease.Linear),
                new OrbitJumpStep(_orbitState.GetRiftExitRadius(), 0.2f, Ease.Linear)
            });

            EventBus<PassedThroughOrbitEvent>.Raise(new PassedThroughOrbitEvent(currentOrbit, isJumpingOut));
        }
        
        private void JumpToOppositeOrbit()
        {
            float targetRadius = _orbitState.GetMovementRadius(_orbitState.OppositeOrbit, _orbitState.CurrentOrbit);

            _jumpTransitionService.Play(new OrbitJumpStep(targetRadius, JUMP_SPEED, Ease.OutQuad));
            _orbitState.Swap();
        }

        private void JumpPartiallyFromGravityWell(GravityWellZone zone)
        {
            float sourceRadius = _orbitState.GetMovementRadius(zone.SourceOrbit, zone.TargetOrbit);
            float targetRadius = _orbitState.GetMovementRadius(zone.TargetOrbit, zone.SourceOrbit);
            float fullGap = Mathf.Max(0.01f, Mathf.Abs(targetRadius - sourceRadius));
            float step = fullGap * Mathf.Clamp01(zone.PartialJumpStep);
            float partialTargetRadius = Mathf.MoveTowards(_movementModel.Radius, targetRadius, step);
            float distance = Mathf.Abs(_movementModel.Radius - partialTargetRadius);
            float duration = Mathf.Max(0.01f, JUMP_SPEED * (distance / fullGap));

            _jumpTransitionService.Play(new OrbitJumpStep(partialTargetRadius, duration, Ease.OutQuad));
        }
        
        private void OnObstacleCollided(ObstacleCollidedEvent @event)
        {
            if (@event.ObstacleType == ArcObjectType.Rift)
            {
                _canGoThroughRift = @event.Entered;
            }
        }
        public void Dispose()
        {
            EventBus<ObstacleCollidedEvent>.Deregister(_onObstacleCollidedEvent);
        }
    }
}
