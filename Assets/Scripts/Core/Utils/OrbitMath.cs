using UnityEngine;

namespace OrbitalEscape.Utils
{
    public static class OrbitMath
    {
        public const float DEGREES_TO_RADIANS = (float)(Mathf.PI / 180.0);
        public const float RADIANS_TO_DEGREES = (float)(180.0 / Mathf.PI);
        
        public static float Circumference(float radius)
        {
            return 2.0f * Mathf.PI * radius;
        }
        
        public static float ArcToRadians(float arcLength, float radius)
        {
            return arcLength / radius;
        }
        
        public static float ArcToDegrees(float arcLength, float radius)
        {
            return ArcToRadians(arcLength, radius) * RADIANS_TO_DEGREES;
        }
        
        public static float DegreesToArc(float angleDegrees, float radius)
        {
            return angleDegrees * DEGREES_TO_RADIANS * radius;
        }

        public static float NormalizeDegrees360(float angleDegrees)
        {
            angleDegrees %= 360.0f;
            if (angleDegrees < 0.0f)
            {
                angleDegrees += 360.0f;
            }
            return angleDegrees;
        }
        
        public static float NormalizeArc(float arc, float circumference)
        {
            float value = arc % circumference;
            if (value < 0.0f)
            {
                value += circumference;
            }

            return value;
        }
    }
}
