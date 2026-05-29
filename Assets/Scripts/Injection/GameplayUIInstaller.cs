using OrbitalEscape.UI;
using OrbitalEscape.Visuals.Palettes;
using Zenject;

namespace OrbitalEscape.Injection
{
    public class GameplayUIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            InstallWindows();
            InstallHUDs();
            Container.Bind<IUIManager>().To<UIManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<PaletteUITextController>().AsSingle().NonLazy();
        }

        private void InstallWindows()
        {
            Container.Bind<IController>().To<RestartWindowController>().AsSingle();
        }
        
        private void InstallHUDs()
        {
            Container.Bind<ScoreHUD>().FromComponentInHierarchy().AsSingle();
        }
    }
}
