namespace OrbitalEscape.Orbits
{
    public interface IRiftClosureState
    {
        bool IsClosed(int orbitIndex);
        bool Close(int orbitIndex);
        void Clear();
    }
}
