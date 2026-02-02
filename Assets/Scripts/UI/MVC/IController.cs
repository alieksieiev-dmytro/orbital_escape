using System;

namespace OrbitalEscape.UI
{
    public interface IController
    {
        IConvertible Type { get; }
        
        event Action<IConvertible, ScreenData> ScreenRequested;
        
        void Initialize(ScreenData screenData);
        void Complete();
    }
}
