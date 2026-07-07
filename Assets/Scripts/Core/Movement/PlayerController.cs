using System;
using OrbitalEscape.Events;
using OrbitalEscape.Orbits;
using OrbitalEscape.UI;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    public class PlayerController : IDisposable
    {
        private readonly PlayerMovementController _playerMovementController;
        private readonly IUIManager _uiManager;
        private readonly Transform _playerTransform;
        private readonly EventBinding<ObstacleCollidedEvent> _onObstacleCollidedEvent;
        private bool _isDead;

        [Inject]
        public PlayerController(Transform playerTransform, PlayerMovementController playerMovementController,
            IUIManager uiManager)
        {
            _onObstacleCollidedEvent = new EventBinding<ObstacleCollidedEvent>(OnObstacleCollidedEvent);
            EventBus<ObstacleCollidedEvent>.Register(_onObstacleCollidedEvent);

            _playerMovementController = playerMovementController;
            _uiManager = uiManager;
            _playerTransform = playerTransform;
        }
        
        public void Dispose() => EventBus<ObstacleCollidedEvent>.Deregister(_onObstacleCollidedEvent);
        
        private void OnObstacleCollidedEvent(ObstacleCollidedEvent @event)
        {
            if (_isDead ||
                !@event.Entered ||
                (@event.ObstacleType != ArcObjectType.Spike &&
                 @event.ObstacleType != ArcObjectType.LaserGate))
            {
                return;
            }

            _isDead = true;
            EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent());
            _playerTransform.gameObject.SetActive(false);
            _playerMovementController.StopMovement();
            _uiManager.OpenWindow(WindowType.Restart, null);
        }
    }
}
