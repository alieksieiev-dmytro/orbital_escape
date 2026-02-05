using OrbitalEscape.Input;
using OrbitalEscape.UI;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Injection
{
    public class LauncherInstaller : MonoInstaller
    {
        private Transform _windowsRoot;
        
        public override void InstallBindings()
        {
            _windowsRoot = FindObjectsByType<WindowRoot>(FindObjectsSortMode.None)[0].transform;
            DontDestroyOnLoad(_windowsRoot.gameObject);
            
            InstallFactory();
            InstallKeyBindings();
            InstallInputController();
        }

        private void InstallFactory()
        {
            Container.Bind(typeof(IViewFactory)).To<ViewFactory>().AsSingle().WithArguments(_windowsRoot, Constants.WINDOW_PREFAB_PATH);
        }
        
        private void InstallKeyBindings()
        {
            Container.Bind<KeyBinding>()
                .FromInstance(new KeyBinding(nameof(KeyCode.R).ToLowerInvariant(), nameof(CommandType.Restart)))
                .AsCached();
            
            Container.Bind<KeyBinding>()
                .FromInstance(new KeyBinding(nameof(KeyCode.Space).ToLowerInvariant(), nameof(CommandType.Jump)))
                .AsCached();

            Container.Bind<KeyBinding>()
                .FromInstance(new KeyBinding(nameof(KeyCode.D).ToLowerInvariant(), nameof(CommandType.MoveClockwise)))
                .AsCached();

            Container.Bind<KeyBinding>()
                .FromInstance(new KeyBinding(nameof(KeyCode.A).ToLowerInvariant(), nameof(CommandType.MoveCounterClockwise)))
                .AsCached();
        }
        
        private void InstallInputController()
        {
            Container.BindInterfacesTo<InputController>().AsSingle().NonLazy();
        }
    }
}
