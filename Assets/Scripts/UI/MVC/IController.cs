using System;

namespace OrbitalEscape.UI
{
    public interface IController
    {
        IConvertible Type { get; }

        void Initialize(ScreenData screenData);
        void Complete();
        void RequestClose();
        void SetInteractionSuspended(bool suspended);
    }
}
