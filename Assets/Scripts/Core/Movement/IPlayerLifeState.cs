namespace OrbitalEscape.Movement
{
    public interface IPlayerLifeState
    {
        bool IsAlive { get; }
        bool TryKill();
    }
}
