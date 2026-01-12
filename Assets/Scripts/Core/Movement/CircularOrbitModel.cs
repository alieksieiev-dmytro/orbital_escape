using UnityEngine;

namespace OrbitalEscape.Movement
{
    public class CircularOrbitModel
    {
        private const float TWO_PI = Mathf.PI * 2.0f;
        private const float MIN_RADIUS = 0.0001f;
        
        private float _angleRadians;
        
        public CircularOrbitModel(float angleRadians)
        {
            _angleRadians = angleRadians;
        }
        
        public void Step(float linearSpeedUnitsPerSec, float radius, float deltaTime, MovementDirection direction)
        {
            float safeRadius = Mathf.Max(radius, MIN_RADIUS);
            float angularSpeed = linearSpeedUnitsPerSec / safeRadius;
            float signedAngularSpeed = angularSpeed * (int)direction;
            
            _angleRadians = WrapAngle(_angleRadians + signedAngularSpeed * deltaTime);
        }

        public Vector2 GetPosition(Vector2 center, float radius)
        {
            float x = Mathf.Cos(_angleRadians);
            float y = Mathf.Sin(_angleRadians);
            return center + new Vector2(x, y) * radius;
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
