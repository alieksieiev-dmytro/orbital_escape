namespace OrbitalEscape.Orbits
{
    public readonly struct GravityWell : IOrbitObject
    {
        public float PartialJumpStep { get; }
        public float PullSpeed { get; }
        public float ReturnDuration { get; }
        public float VisualAlpha { get; }
        public ArcObjectType Type => ArcObjectType.GravityWell;

        public GravityWell(
            float partialJumpStep,
            float pullSpeed,
            float returnDuration,
            float visualAlpha)
        {
            PartialJumpStep = partialJumpStep;
            PullSpeed = pullSpeed;
            ReturnDuration = returnDuration;
            VisualAlpha = visualAlpha;
        }
    }
}
