using DG.Tweening;

namespace OrbitalEscape.Movement
{
    public readonly struct OrbitJumpStep
    {
        public float TargetRadius { get; }
        public float Duration { get; }
        public Ease Ease { get; }

        public OrbitJumpStep(float targetRadius, float duration, Ease ease)
        {
            TargetRadius = targetRadius;
            Duration = duration;
            Ease = ease;
        }
    }
}
