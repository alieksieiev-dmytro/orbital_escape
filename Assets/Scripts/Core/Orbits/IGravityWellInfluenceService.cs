namespace OrbitalEscape.Orbits
{
    public interface IGravityWellInfluenceService
    {
        bool TryGetActiveZone(out GravityWellZone zone);
        void SuppressActiveUntilExit();
        void ClearState();
    }
}
