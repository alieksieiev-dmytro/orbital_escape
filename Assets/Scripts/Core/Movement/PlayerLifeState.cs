namespace OrbitalEscape.Movement
{
    public sealed class PlayerLifeState : IPlayerLifeState
    {
        public bool IsAlive { get; private set; } = true;

        public bool TryKill()
        {
            if (!IsAlive)
            {
                return false;
            }

            IsAlive = false;
            return true;
        }
    }
}
