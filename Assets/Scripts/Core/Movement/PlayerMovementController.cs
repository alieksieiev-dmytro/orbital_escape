using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    public class PlayerMovementController : ITickable
    {
        private readonly IMovementSpeedService _movementSpeedService;
        private readonly Transform _playerTransform;
        private readonly Vector2 _center;
        private readonly IMovementModel _movementModel;
        
        private MovementDirection _currentDirection;
        private bool _isMoving;

        [Inject]
        public PlayerMovementController(
            Transform playerTransform,
            IMovementModel movementModel,
            [Inject(Id = Constants.INJECTION_ID_CENTRE)] Vector2 center,
            IMovementSpeedService movementSpeedService)
        {
            _playerTransform = playerTransform;
            _movementModel = movementModel;
            _center = center;
            _movementSpeedService = movementSpeedService;
            _isMoving = true;
            SetMovementDirection(MovementDirection.Clockwise);
        }

        public void Tick()
        {
            if (!_isMoving)
                return;
            
            _movementModel.Step(_movementSpeedService.CurrentLinearSpeed, Time.deltaTime, _currentDirection);
            RefreshTransform();
        }

        public void RefreshTransform()
        {
            var newPos = _movementModel.GetPosition(_center);
            _playerTransform.position = new Vector3(newPos.x, newPos.y, _playerTransform.position.z);
            
            var radiusVector = newPos - _center;
            var tangentDirection = _currentDirection == MovementDirection.Clockwise
                ? new Vector2(radiusVector.y, -radiusVector.x)
                : new Vector2(-radiusVector.y, radiusVector.x);

            float angle = Mathf.Atan2(tangentDirection.y, tangentDirection.x) * Mathf.Rad2Deg;
            _playerTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        
        public void SetMovementDirection(MovementDirection direction)
        {
            _currentDirection = direction;
        }
        
        public void StopMovement()
        {
            _isMoving = false;
        }
    }
}
