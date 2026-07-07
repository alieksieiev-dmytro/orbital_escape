using System;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct ImpulseTrapGenerationSettings
    {
        public float Density { get; }
        public float TrapArcLength { get; }
        public float Height { get; }

        public ImpulseTrapGenerationSettings(float density, float trapArcLength, float height)
        {
            Density = Math.Min(Math.Max(density, 0.0f), 1.0f);
            TrapArcLength = trapArcLength;
            Height = height;
        }
    }
}
