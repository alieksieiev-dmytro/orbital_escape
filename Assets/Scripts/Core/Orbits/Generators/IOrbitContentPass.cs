namespace OrbitalEscape.Orbits.Generators
{
    public interface IOrbitContentPass
    {
        int Order { get; }
        void Populate(Orbit orbit, LevelBuildContext context);
    }
}
