using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace OrbitalEscape.UI
{
    public class UIManager : IUIManager
    {
        private readonly List<IController> _windowControllers;
        
        [Inject]
        public UIManager(DiContainer diContainer)
        {
            _windowControllers = diContainer.ResolveAll<IController>().ToList();
        }

        public void OpenWindow(IConvertible windowKey, ScreenData screenData)
        {
            var controller = GetWindow(windowKey);
            controller.Initialize(screenData);
        }
        
        public void CloseWindow(IConvertible windowKey)
        {
            throw new NotImplementedException();
        }

        private IController GetWindow(IConvertible windowKey) =>
            _windowControllers.FirstOrDefault(controller => controller.Type.Equals(windowKey));
    }
}
