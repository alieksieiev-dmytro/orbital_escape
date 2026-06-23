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
        private const float BASE_JUMP_DURATION = 0.4f;
        private const float BASE_RIFT_JUMP_STEP_DURATION = 0.2f;
        
        private readonly IMovementModel _movementModel;
        private readonly PlayerOrbitState _orbitState;
        private readonly IGravityWellInfluenceService _gravityWellInfluenceService;
        private readonly IOrbitJumpTransitionService _jumpTransitionService;
        private readonly IJumpSpeedService _jumpSpeedService;
        private readonly EventBinding<ObstacleCollidedEvent> _onObstacleCollidedEvent;

        private bool _canGoThroughRift;
        private float _jumpInputUnlockTime;

        [Inject]
        public JumpBehaviour(
            IMovementModel movementModel,
            PlayerOrbitState orbitState,
            IGravityWellInfluenceService gravityWellInfluenceService,
            IOrbitJumpTransitionService jumpTransitionService,
            IJumpSpeedService jumpSpeedService)
        {
            _movementModel = movementModel;
            _orbitState = orbitState;
            _gravityWellInfluenceService = gravityWellInfluenceService;
            _jumpTransitionService = jumpTransitionService;
            _jumpSpeedService = jumpSpeedService;
            
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
            float stepDuration = _jumpSpeedService.ScaleDuration(BASE_RIFT_JUMP_STEP_DURATION);
            _canGoThroughRift = false;

            _jumpTransitionService.Play(new[]
            {
                new OrbitJumpStep(currentOrbit.Radius + jumpHeight, stepDuration, Ease.Linear),
                new OrbitJumpStep(_orbitState.GetRiftExitRadius(), stepDuration, Ease.Linear)
            });

            EventBus<PassedThroughOrbitEvent>.Raise(new PassedThroughOrbitEvent(currentOrbit, isJumpingOut));
        }
        
        private void JumpToOppositeOrbit()
        {
            float targetRadius = _orbitState.GetMovementRadius(_orbitState.OppositeOrbit, _orbitState.CurrentOrbit);

            _jumpTransitionService.Play(new OrbitJumpStep(
                targetRadius,
                _jumpSpeedService.ScaleDuration(BASE_JUMP_DURATION),
                Ease.OutQuad));
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
            float baseDuration = BASE_JUMP_DURATION * (distance / fullGap);

            _jumpTransitionService.Play(new OrbitJumpStep(
                partialTargetRadius,
                _jumpSpeedService.ScaleDuration(baseDuration),
                Ease.OutQuad));
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
