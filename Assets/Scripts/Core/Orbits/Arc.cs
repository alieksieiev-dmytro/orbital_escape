using System;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits
{
    public readonly struct Arc : IEquatable<Arc>
    {
        public float StartAngleDegree { get; }
        public float EndAngleDegree { get; }
        public float Radius { get; }
        
        public Arc(float startAngleDegree, float endAngleDegree, float radius)
        {
            StartAngleDegree = startAngleDegree;
            EndAngleDegree = endAngleDegree;
            Radius = radius;
        }

        public float Length()
        {
            float startNormalized = OrbitMath.NormalizeDegrees360(StartAngleDegree);
            float endNormalized = OrbitMath.NormalizeDegrees360(EndAngleDegree);
            
            float delta = endNormalized >= startNormalized
                ? endNormalized - startNormalized
                : 360.0f - (startNormalized - endNormalized);
            
            float deltaRadians = delta * OrbitMath.DEGREES_TO_RADIANS;
            return deltaRadians * Radius;
        }
        
        public bool IsOverlapping(Arc other)
        {
            float aArcStart = OrbitMath.NormalizeDegrees360(StartAngleDegree);
            float aArcEnd = OrbitMath.NormalizeDegrees360(EndAngleDegree);
            
            float bArcStart = OrbitMath.NormalizeDegrees360(other.StartAngleDegree);
            float bArcEnd = OrbitMath.NormalizeDegrees360(other.EndAngleDegree);
            
            bool aWraps = aArcStart > aArcEnd;
            bool bWraps = bArcStart > bArcEnd;
            
            if (!aWraps && !bWraps)
            {
                return aArcStart <= bArcEnd && bArcStart <= aArcEnd;
            }

            if (aWraps && bWraps)
            {
                return true;
            }

            if (aWraps)
            {
                return aArcStart <= bArcEnd || bArcStart <= aArcEnd;
            }

            return bArcStart <= aArcEnd || aArcStart <= bArcEnd;
        }

        #region IEquatable Implementation
        
        public bool Equals(Arc other)
        {
            return StartAngleDegree.Equals(other.StartAngleDegree) && EndAngleDegree.Equals(other.EndAngleDegree);
        }
        
        public override bool Equals(object obj)
        {
            return obj is Arc other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(StartAngleDegree, EndAngleDegree);
        }

        #endregion
    }
}
