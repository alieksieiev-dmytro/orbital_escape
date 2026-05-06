namespace OrbitalEscape.Orbits
{
    public readonly struct ImpulseTrap : IOrbitObject
    {
        public float Height { get; }

        public ArcObjectType Type => ArcObjectType.ImpulseTrap;

        public ImpulseTrap(float height)
        {
            Height = height;
        }
    }
}
