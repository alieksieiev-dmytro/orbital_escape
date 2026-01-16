using System;
using System.Collections.Generic;
using OrbitalEscape.Orbits;
using UnityEngine;

namespace OrbitalEscape.Movement
{
    public class CollisionDetector
    {
        private readonly Vector2 _centre;
        private Orbit _currentOrbit;
        
        public CollisionDetector(Vector2 centre)
        {
            _centre = centre;
        }

        public bool IsOnRift(ICollection<Rift> rifts, Vector2 point, float orbitRadius)
        {
            if (rifts == null || rifts.Count == 0) return false;
            
            Vector2 d = point - _centre;
            float dist = d.magnitude;
            
            if (Mathf.Abs(dist - orbitRadius) > Constants.RIFT_COLLISION_TOLERANCE)
            {
                return false;
            }

            float pointAngle = NormalizeDegrees360(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            
            foreach (var rift in rifts)
            {
                if (IsAngleOnRift(pointAngle, rift.StartAngleDegrees, ArcLengthToDegrees(rift.ArcLength, orbitRadius),
                    Constants.ANGLE_TOLERANCE_DEGREES))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private float NormalizeDegrees360(float degrees) => Mathf.Repeat(degrees, 360.0f);    
        
        private bool IsAngleOnRift(float angle, float startDegrees, float lengthDegrees, float toleranceDegrees)
        {
            float start = NormalizeDegrees360(startDegrees);
            float length = Mathf.Max(0.0f, lengthDegrees);

            if (length >= 360.0f - toleranceDegrees)
            {
                return true;
            }

            if (length <= toleranceDegrees)
            {
                float delta = Mathf.DeltaAngle(start, angle);
                return Mathf.Abs(delta) <= toleranceDegrees;
            }

            float end = NormalizeDegrees360(start + length);

            if (start <= end)
            {
                return angle >= start - toleranceDegrees && angle <= end + toleranceDegrees;
            }

            return angle >= start - toleranceDegrees || angle <= end + toleranceDegrees;
        }    
        
        private float ArcLengthToDegrees(float arcLength, float radius)
        {
            if (radius <= Mathf.Epsilon) return 0.0f;

            float angleRadians = Mathf.Max(0.0f, arcLength) / radius;
            return angleRadians * Mathf.Rad2Deg;
        }
    }
}
