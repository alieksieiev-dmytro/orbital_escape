using OrbitalEscape.UI;
using Zenject;

namespace OrbitalEscape.Injection
{
    public class GameplayUIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            InstallWindows();
            Container.Bind<IUIManager>().To<UIManager>().AsSingle();
        }

        private void InstallWindows()
        {
            Container.Bind<IController>().To<RestartWindowController>().AsSingle();
        }
    }
}
