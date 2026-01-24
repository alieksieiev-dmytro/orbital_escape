namespace OrbitalEscape.Orbits
{
    public struct SpikeObstacle : IOrbitObject
    {
        public float Height { get; }
        
        public OrbitObjectType Type => OrbitObjectType.Spike;
        
        public SpikeObstacle(float height)
        {
            Height = height;
        }
    }
}
