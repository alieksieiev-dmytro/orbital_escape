using OrbitalEscape.Events;
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
        private readonly MovementSpeedConfig _movementSpeedConfig;
        
        private MovementDirection _requestedDirection = MovementDirection.Clockwise;
        private float _directionFactor = (float)MovementDirection.Clockwise;
        private bool _isMoving;

        [Inject]
        public PlayerMovementController(
            Transform playerTransform,
            IMovementModel movementModel,
            [Inject(Id = Constants.INJECTION_ID_CENTRE)] Vector2 center,
            IMovementSpeedService movementSpeedService,
            MovementSpeedConfig movementSpeedConfig)
        {
            _playerTransform = playerTransform;
            _movementModel = movementModel;
            _center = center;
            _movementSpeedService = movementSpeedService;
            _movementSpeedConfig = movementSpeedConfig;
            _isMoving = true;
        }

        public void Tick()
        {
            if (!_isMoving)
                return;
            
            UpdateMovementDirection(Time.deltaTime);

            MovementDirection movementDirection = _directionFactor < 0.0f
                ? MovementDirection.Clockwise
                : MovementDirection.CounterClockwise;
            float linearSpeed = _movementSpeedService.CurrentLinearSpeed * Mathf.Abs(_directionFactor);

            _movementModel.Step(linearSpeed, Time.deltaTime, movementDirection);
            RefreshTransform();
        }

        public void RefreshTransform()
        {
            var newPos = _movementModel.GetPosition(_center);
            _playerTransform.position = new Vector3(newPos.x, newPos.y, _playerTransform.position.z);
            
            var radiusVector = newPos - _center;
            var tangentDirection = new Vector2(radiusVector.y, -radiusVector.x);

            float angle = Mathf.Atan2(tangentDirection.y, tangentDirection.x) * Mathf.Rad2Deg;
            _playerTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        
        public void SetMovementDirection(MovementDirection direction)
        {
            if (_requestedDirection == direction)
            {
                return;
            }

            _requestedDirection = direction;
            EventBus<MovementDirectionChangedEvent>.Raise(new MovementDirectionChangedEvent(direction));
        }
        
        public void StopMovement()
        {
            _isMoving = false;
        }

        public void ResetForRun(float radius)
        {
            _movementModel.Reset(0f, radius);
            _requestedDirection = MovementDirection.Clockwise;
            _directionFactor = (float)_requestedDirection;
            _isMoving = true;
            RefreshTransform();
            EventBus<MovementDirectionChangedEvent>.Raise(new MovementDirectionChangedEvent(_requestedDirection));
        }

        private void UpdateMovementDirection(float deltaTime)
        {
            float targetFactor = (float)_requestedDirection;
            float duration = _movementSpeedConfig.DirectionChangeDuration;
            _directionFactor = duration <= 0.0f
                ? targetFactor
                : Mathf.MoveTowards(_directionFactor, targetFactor, 2.0f * deltaTime / duration);
        }
    }
}
