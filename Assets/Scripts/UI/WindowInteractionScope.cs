using System;

namespace OrbitalEscape.UI
{
    public sealed class WindowInteractionScope : IDisposable
    {
        private Action _release;

        public WindowInteractionScope(Action release) => _release = release;

        public void Dispose()
        {
            Action release = _release;
            _release = null;
            release?.Invoke();
        }
    }
}
