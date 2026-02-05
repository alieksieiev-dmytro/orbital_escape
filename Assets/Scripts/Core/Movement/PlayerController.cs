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
        private readonly EventBinding<OnObstacleCollidedEvent> _onObstacleCollidedEvent;

        [Inject]
        public PlayerController(Transform playerTransform, PlayerMovementController playerMovementController,
            IUIManager uiManager)
        {
            _onObstacleCollidedEvent = new EventBinding<OnObstacleCollidedEvent>(OnObstacleCollidedEvent);
            EventBus<OnObstacleCollidedEvent>.Register(_onObstacleCollidedEvent);

            _playerMovementController = playerMovementController;
            _uiManager = uiManager;
            _playerTransform = playerTransform;
        }
        
        public void Dispose() => EventBus<OnObstacleCollidedEvent>.Deregister(_onObstacleCollidedEvent);
        
        private void OnObstacleCollidedEvent(OnObstacleCollidedEvent @event)
        {
            if (@event.ObstacleType != ArcObjectType.Spike || !@event.Entered)
                return;
            
            _playerTransform.gameObject.SetActive(false);
            _playerMovementController.StopMovement();
            _uiManager.OpenWindow(WindowType.Restart, null);
        }
    }
}
