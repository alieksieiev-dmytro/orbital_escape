namespace OrbitalEscape.Orbits
{
    public struct SpikeObstacle : IOrbitObject
    {
        public float Height { get; }
        
        public ArcObjectType Type => ArcObjectType.Spike;
        
        public SpikeObstacle(float height)
        {
            Height = height;
        }
    }
}
