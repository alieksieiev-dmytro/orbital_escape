using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace OrbitalEscape.UI
{
    public class UIManager : IUIManager
    {
        private readonly DiContainer _diContainer;
        private List<IController> _windowControllers;
        
        [Inject]
        public UIManager(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public void OpenWindow(IConvertible windowKey, ScreenData screenData)
        {
            var controller = GetWindow(windowKey);
            
            if (controller == null)
            {
                throw new InvalidOperationException($"Window controller '{windowKey}' is not bound.");
            }
            
            controller.Initialize(screenData);
        }
        
        public void CloseWindow(IConvertible windowKey)
        {
            var controller = GetWindow(windowKey);
            controller?.Complete();
        }

        private IController GetWindow(IConvertible windowKey)
        {
            _windowControllers ??= _diContainer.ResolveAll<IController>().ToList();
            return _windowControllers.FirstOrDefault(controller => controller.Type.Equals(windowKey));
        }
    }
}
