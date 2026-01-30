using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    public class PlayerMovementController : ITickable
    {
        private readonly float _linearSpeedPerSecond;
        private readonly Transform _playerTransform;
        
        [Inject(Id = Constants.INJECTION_ID_CENTRE)] private Vector2 _center;
        [Inject] private IMovementModel _movementModel;
        
        private MovementDirection _currentDirection;
        private bool _isMoving;

        public PlayerMovementController(Transform playerTransform, float linearSpeedPerSecond,
            IMovementModel movementModel)
        {
            _playerTransform = playerTransform;
            _linearSpeedPerSecond = linearSpeedPerSecond;
            _movementModel = movementModel;
            _isMoving = true;
            SetMovementDirection(MovementDirection.Clockwise);
        }

        public void Tick()
        {
            if (!_isMoving)
                return;
            
            _movementModel.Step(_linearSpeedPerSecond, Time.deltaTime, _currentDirection);
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
