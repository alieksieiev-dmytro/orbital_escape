using System;

namespace OrbitalEscape.UI
{
    public interface IUIManager
    {
        bool HasOpenWindow { get; }
        void OpenWindow(IConvertible windowKey, ScreenData screenData);
        void CloseWindow(IConvertible windowKey);
        void CloseActiveWindow();
        IDisposable SuspendInteraction();
    }
}
