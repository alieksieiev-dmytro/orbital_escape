using System;

namespace OrbitalEscape.UI
{
    public interface IUIManager
    {
        void OpenWindow(IConvertible windowKey, ScreenData screenData);
        void CloseWindow(IConvertible windowKey);
    }
}
