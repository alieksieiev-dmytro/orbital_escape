using OrbitalEscape.Cheats;
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
            Container.BindInterfacesTo<UIManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PaletteFontMaterialController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PaletteUIViewTargetController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<MovementSpeedCheatInfoProvider>().AsSingle();
            Container.BindInterfacesAndSelfTo<DifficultyCheatInfoProvider>().AsSingle();
            Container.BindInterfacesAndSelfTo<SpikePoolCheatInfoProvider>().AsSingle();
            Container.BindInterfacesAndSelfTo<CheatInfoHUDController>().AsSingle().NonLazy();
        }

        private void InstallWindows()
        {
            Container.Bind<IController>().To<RestartWindowController>().AsSingle();
            Container.Bind<IController>().To<PauseWindowController>().AsSingle();
            Container.Bind<IController>().To<CheatCommandWindowController>().AsSingle();
        }
        
        private void InstallHUDs()
        {
            Container.Bind<ScoreHUD>().FromComponentInHierarchy().AsSingle();
        }
    }
}
