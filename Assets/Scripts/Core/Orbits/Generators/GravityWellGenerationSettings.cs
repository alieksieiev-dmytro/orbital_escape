using System;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct GravityWellGenerationSettings
    {
        public float Density { get; }
        public float FieldArcLength { get; }
        public float PartialJumpStep { get; }
        public float PullSpeed { get; }
        public float ReturnDuration { get; }
        public float VisualAlpha { get; }

        public GravityWellGenerationSettings(
            float density,
            float fieldArcLength,
            float partialJumpStep,
            float pullSpeed,
            float returnDuration,
            float visualAlpha)
        {
            Density = Math.Min(Math.Max(density, 0.0f), 1.0f);
            FieldArcLength = fieldArcLength;
            PartialJumpStep = Math.Min(Math.Max(partialJumpStep, 0.0f), 1.0f);
            PullSpeed = Math.Max(0.0f, pullSpeed);
            ReturnDuration = Math.Max(0.01f, returnDuration);
            VisualAlpha = Math.Min(Math.Max(visualAlpha, 0.0f), 1.0f);
        }
    }
}
