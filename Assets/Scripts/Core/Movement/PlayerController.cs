using OrbitalEscape.Events;
using OrbitalEscape.Orbits;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    public class PlayerController
    {
        private PlayerMovementController _playerMovementController;
        
        [Inject]
        public void Construct(PlayerMovementController playerMovementController)
        {
            _playerMovementController = playerMovementController;
        }
        
        private readonly Transform _playerTransform;
        
        public PlayerController(Transform playerTransform)
        {
            var onObstacleCollidedEvent = new EventBinding<OnObstacleCollidedEvent>(OnObstacleCollidedEvent);
            EventBus<OnObstacleCollidedEvent>.Register(onObstacleCollidedEvent);
            _playerTransform = playerTransform;
        }
        
        private void OnObstacleCollidedEvent(OnObstacleCollidedEvent @event)
        {
            if (@event.ObstacleType == ArcObjectType.Spike)
            {
                _playerTransform.gameObject.SetActive(false);
                _playerMovementController.StopMovement();
            }
        }
    }
}
