namespace OrbitalEscape.Input
{
    public sealed class InputGate : IInputGate
    {
        private int _lockCount;

        public bool IsLocked => _lockCount > 0;

        public void Lock()
        {
            _lockCount++;
        }

        public void Unlock()
        {
            if (_lockCount > 0)
            {
                _lockCount--;
            }
        }
    }
}
