using DG.Tweening;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    public class PlayerMovementController : ITickable
    {
        private readonly float _linearSpeedPerSecond;
        private readonly Vector2 _center;
        private readonly CircularOrbitModel _orbitModel;
        private readonly Transform _playerTransform;
        
        private float _radius;
        private MovementDirection _currentDirection;

        public PlayerMovementController(Transform playerTransform, Vector2 center, float radius,
            float linearSpeedPerSecond)
        {
            _playerTransform = playerTransform;
            _center = center;
            _radius = radius;
            _linearSpeedPerSecond = linearSpeedPerSecond;
            _orbitModel = new CircularOrbitModel(0.0f);
            SetMovementDirection(MovementDirection.Clockwise);
        }

        public void Tick()
        {
            _orbitModel.Step(_linearSpeedPerSecond, _radius, Time.deltaTime, _currentDirection);
            var newPos = _orbitModel.GetPosition(_center, _radius);
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

        public void SetRadius(float radius)
        {
            Tween radiusTween = DOTween.To(() => _radius, x => _radius = x, radius, 0.5f).SetEase(Ease.OutQuad);
            radiusTween.Play();
        }
    }
}
