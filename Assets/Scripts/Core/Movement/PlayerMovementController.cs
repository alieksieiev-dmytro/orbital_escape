using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    public class PlayerMovementController : ITickable
    {
        private readonly float _radius;
        private readonly float _linearSpeedPerSecond;
        private readonly Vector2 _center;
        private readonly CircularOrbitModel _orbitModel;
        public Transform _playerTransform;
        
        private MovementDirection _currentDirection;

        public PlayerMovementController(Transform playerTransform, Vector2 center, float radius,
            float linearSpeedPerSecond)
        {
            _playerTransform = playerTransform;
            _center = center;
            _radius = radius;
            _linearSpeedPerSecond = linearSpeedPerSecond;
            _orbitModel = new CircularOrbitModel(0.0f);
            _currentDirection = MovementDirection.Clockwise;
        }

        public void Tick()
        {
            _orbitModel.Step(_linearSpeedPerSecond, _radius, Time.deltaTime, _currentDirection);
            var newPos = _orbitModel.GetPosition(_center, _radius);
            _playerTransform.position = new Vector3(newPos.x, newPos.y, _playerTransform.position.z);
        }
        
        public void SetMovementDirection(MovementDirection direction)
        {
            _currentDirection = direction;
        }
    }
}
