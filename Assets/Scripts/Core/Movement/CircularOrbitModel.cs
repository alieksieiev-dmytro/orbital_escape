using UnityEngine;

namespace OrbitalEscape.Movement
{
    public class CircularOrbitModel : IMovementModel
    {
        private const float TWO_PI = Mathf.PI * 2.0f;
        private const float MIN_RADIUS = 0.0001f;
        
        private float _angleRadians;
        private float _radius;
        
        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(value, MIN_RADIUS);
        }

        public float AngleRadians => _angleRadians;
        
        public CircularOrbitModel(float angleRadians, float radius)
        {
            _angleRadians = angleRadians;
            _radius = radius;
        }
        
        public void Step(float linearSpeedUnitsPerSec, float deltaTime, MovementDirection direction)
        {
            float safeRadius = Mathf.Max(_radius, MIN_RADIUS);
            float angularSpeed = linearSpeedUnitsPerSec / safeRadius;
            float signedAngularSpeed = angularSpeed * (int)direction;
            
            _angleRadians = WrapAngle(_angleRadians + signedAngularSpeed * deltaTime);
        }

        public Vector2 GetPosition(Vector2 center)
        {
            float x = Mathf.Cos(_angleRadians);
            float y = Mathf.Sin(_angleRadians);
            return center + new Vector2(x, y) * _radius;
        }

        private float WrapAngle(float angleRad)
        {
            angleRad %= TWO_PI;
            if (angleRad < 0.0f)
            {
                angleRad += TWO_PI;
            }

            return angleRad;
        }
    }
}
