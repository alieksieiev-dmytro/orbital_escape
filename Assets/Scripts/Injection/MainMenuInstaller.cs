using OrbitalEscape.Audio;
using OrbitalEscape.Input;
using OrbitalEscape.UI;
using OrbitalEscape.Visuals.Palettes;
using UnityEngine;
using Zenject;
using static OrbitalEscape.Constants;

namespace OrbitalEscape.Injection
{
    public class MainMenuInstaller : MonoInstaller
    {
        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private Transform[] _environmentRoots;

        public override void InstallBindings()
        {
            InstallPalettes();
            InstallWindows();
            InstallCommands();

            Container.BindInterfacesTo<UIManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<MainMenuStartupController>().AsSingle().NonLazy();
            Container.BindInterfacesTo<MainMenuAudioController>().AsSingle().NonLazy();
        }

        private void InstallPalettes()
        {
            Container.BindInterfacesAndSelfTo<MainMenuPaletteService>().AsSingle().NonLazy();
            Container.Bind<IPaletteObjectPainter>().To<PaletteObjectPainter>().AsSingle();
            Container.Bind<MainMenuPaletteSceneReferences>()
                .FromInstance(new MainMenuPaletteSceneReferences(_backgroundRenderer, _environmentRoots))
                .AsSingle();
            Container.BindInterfacesAndSelfTo<MainMenuPaletteController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PaletteFontMaterialController>().AsSingle().NonLazy();
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
