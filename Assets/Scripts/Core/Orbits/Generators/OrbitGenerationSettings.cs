namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct OrbitGenerationSettings
    {
        public float FirstOrbitRadius { get; }
        public float OrbitRadiusStep { get; }

        public OrbitGenerationSettings(float firstOrbitRadius, float orbitRadiusStep)
        {
            FirstOrbitRadius = firstOrbitRadius;
            OrbitRadiusStep = orbitRadiusStep;
        }
    }
}
