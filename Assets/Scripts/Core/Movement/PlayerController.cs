using System;
using OrbitalEscape.Events;
using OrbitalEscape.Input;
using OrbitalEscape.Orbits;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    public class PlayerController : IDisposable
    {
        private readonly PlayerMovementController _playerMovementController;
        private readonly IPlayerLifeState _lifeState;
        private readonly IOrbitJumpTransitionService _jumpTransitionService;
        private readonly IGravityWellInfluenceService _gravityWellInfluenceService;
        private readonly IInputGate _inputGate;
        private readonly Collider2D[] _colliders;
        private readonly EventBinding<ObstacleCollidedEvent> _onObstacleCollidedEvent;
        private bool _ownsInputLock;

        [Inject]
        public PlayerController(Transform playerTransform, PlayerMovementController playerMovementController,
            IPlayerLifeState lifeState, IOrbitJumpTransitionService jumpTransitionService,
            IGravityWellInfluenceService gravityWellInfluenceService, IInputGate inputGate)
        {
            _onObstacleCollidedEvent = new EventBinding<ObstacleCollidedEvent>(OnObstacleCollidedEvent);
            EventBus<ObstacleCollidedEvent>.Register(_onObstacleCollidedEvent);

            _playerMovementController = playerMovementController;
            _lifeState = lifeState;
            _jumpTransitionService = jumpTransitionService;
            _gravityWellInfluenceService = gravityWellInfluenceService;
            _inputGate = inputGate;
            _colliders = playerTransform.GetComponentsInChildren<Collider2D>(true);
        }
        
        public void Dispose()
        {
            EventBus<ObstacleCollidedEvent>.Deregister(_onObstacleCollidedEvent);

            if (_ownsInputLock)
            {
                _inputGate.Unlock();
                _ownsInputLock = false;
            }
        }
        
        private void OnObstacleCollidedEvent(ObstacleCollidedEvent @event)
        {
            if (!@event.Entered ||
                (@event.ObstacleType != ArcObjectType.Spike &&
                 @event.ObstacleType != ArcObjectType.LaserGate))
            {
                return;
            }

            if (!_lifeState.TryKill())
            {
                return;
            }

            _inputGate.Lock();
            _ownsInputLock = true;
            _playerMovementController.StopMovement();
            _jumpTransitionService.Stop();
            _gravityWellInfluenceService.ClearState();

            foreach (Collider2D playerCollider in _colliders)
            {
                playerCollider.enabled = false;
            }

            EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent());
        }
    }
}
