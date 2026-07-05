using OrbitalEscape.Audio;
using OrbitalEscape.Input;
using OrbitalEscape.UI;
using Zenject;
using static OrbitalEscape.Constants;

namespace OrbitalEscape.Injection
{
    public class MainMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            InstallWindows();
            InstallCommands();

            Container.Bind<IUIManager>().To<UIManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<MainMenuStartupController>().AsSingle().NonLazy();
            Container.BindInterfacesTo<MainMenuAudioController>().AsSingle().NonLazy();
        }

        private void InstallWindows()
        {
            Container.Bind<IController>().To<MainMenuWindowController>().AsSingle();
            Container.Bind<IController>().To<SelectGameModeWindowController>().AsSingle();
        }

        private void InstallCommands()
        {
            InstallCommand<PlayCommand>(nameof(CommandType.Play), SCENE_GAMEPLAY);
            InstallCommand<ExitCommand>(nameof(CommandType.Exit));
        }

        private void InstallCommand<TCommand>(string id, params object[] args) where TCommand : Command
        {
            Container.BindInterfacesAndSelfTo<TCommand>().AsSingle().WithArguments(args).NonLazy();
            Container.Bind<Command>().WithId(id).FromResolveGetter<TCommand>(x => x);
        }
    }
}
