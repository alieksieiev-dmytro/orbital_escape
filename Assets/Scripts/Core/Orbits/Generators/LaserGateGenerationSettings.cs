using System;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct LaserGateGenerationSettings
    {
        public float Width { get; }
        public float PassTime { get; }
        public float ChargingTime { get; }
        public float ActiveTime { get; }
        public float LowerBaseYOffset { get; }
        public float UpperBaseYOffset { get; }

        public LaserGateGenerationSettings(
            float width,
            float passTime,
            float chargingTime,
            float activeTime,
            float lowerBaseYOffset,
            float upperBaseYOffset)
        {
            Width = Math.Max(0.01f, width);
            PassTime = Math.Max(0.0f, passTime);
            ChargingTime = Math.Max(0.0f, chargingTime);
            ActiveTime = Math.Max(0.0f, activeTime);
            LowerBaseYOffset = lowerBaseYOffset;
            UpperBaseYOffset = upperBaseYOffset;
        }
    }
}
