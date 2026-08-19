namespace OrbitalEscape.Input
{
    public interface IInputGate
    {
        bool IsLocked { get; }
        void Lock();
        void Unlock();
    }
}
