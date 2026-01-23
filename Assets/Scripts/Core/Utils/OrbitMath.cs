using UnityEngine;

namespace OrbitalEscape.Utils
{
    public static class OrbitMath
    {
        public const float DEGREES_TO_RADIANS = (float)(Mathf.PI / 180.0);
        public const float RADIANS_TO_DEGREES = (float)(180.0 / Mathf.PI);

        public static float NormalizeDegrees360(float angleDegrees)
        {
            angleDegrees %= 360.0f;
            if (angleDegrees < 0.0f)
            {
                angleDegrees += 360.0f;
            }
            return angleDegrees;
        }
        
        
        public static (float start, float end) NormalizeInterval(float startAngleDegree, float endAngleDegree)
        {
            float start = NormalizeDegrees360(startAngleDegree);
            float end = NormalizeDegrees360(endAngleDegree);

            if (end < start)
                end += 360f;

            return (start, end);
        }
    }
}
