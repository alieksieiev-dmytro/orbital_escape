namespace OrbitalEscape.Orbits
{
    public readonly struct GravityWellZone
    {
        public int Id { get; }
        public Orbit SourceOrbit { get; }
        public Orbit TargetOrbit { get; }
        public ArcType SourceSide { get; }
        public Arc Arc { get; }
        public float PartialJumpStep { get; }
        public float PullSpeed { get; }
        public float ReturnDuration { get; }

        public GravityWellZone(
            int id,
            Orbit sourceOrbit,
            Orbit targetOrbit,
            ArcType sourceSide,
            Arc arc,
            float partialJumpStep,
            float pullSpeed,
            float returnDuration)
        {
            Id = id;
            SourceOrbit = sourceOrbit;
            TargetOrbit = targetOrbit;
            SourceSide = sourceSide;
            Arc = arc;
            PartialJumpStep = partialJumpStep;
            PullSpeed = pullSpeed;
            ReturnDuration = returnDuration;
        }
    }
}
