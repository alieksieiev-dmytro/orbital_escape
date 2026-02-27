using OrbitalEscape.UI;
using Zenject;

namespace OrbitalEscape.Injection
{
    public class MainMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            InstallWindows();
            Container.Bind<IUIManager>().To<UIManager>().AsSingle();
        }

        private void InstallWindows()
        {
            throw new System.NotImplementedException("Main menu windows not implemented yet");
        }
    }
}
