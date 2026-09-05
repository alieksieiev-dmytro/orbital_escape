using UnityEngine;

namespace OrbitalEscape.Input
{
    public sealed class InputGate : IInputGate
    {
        private int _lockCount;
        private int _suppressedFrame = -1;

        public bool IsLocked => _lockCount > 0 || _suppressedFrame == Time.frameCount;

        public void SuppressForCurrentFrame()
        {
            _suppressedFrame = Time.frameCount;
        }

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
