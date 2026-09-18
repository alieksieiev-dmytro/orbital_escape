using System;
using System.Collections.Generic;
using OrbitalEscape.Audio;
using OrbitalEscape.Cheats;
using OrbitalEscape.Data;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Input;
using OrbitalEscape.Movement;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Orbits.Generators.Patterns;
using OrbitalEscape.Pooling;
using OrbitalEscape.UI;
using OrbitalEscape.Visuals;
using OrbitalEscape.Visuals.Palettes;
using UnityEngine;
using Zenject;
using static OrbitalEscape.Constants;

namespace OrbitalEscape.Injection
{
    public class LauncherInstaller : MonoInstaller
    {
        private Transform _windowsRoot;
        
        public override void InstallBindings()
        {
            _windowsRoot = FindObjectsByType<WindowRoot>(FindObjectsSortMode.None)[0].transform;
            DontDestroyOnLoad(_windowsRoot.root.gameObject);

            InstallConfigs();
            InstallData();
            InstallDifficulty();
            InstallFactory();
            InstallAudio();
            InstallKeyBindings();
            InstallInputController();
        }

        private void InstallData()
        {
            var dataSectionBindings = new List<(string key, Type dataType)>
            {
                (PLAYER_DATA_SCORE_SECTION, typeof(ScoreData)),
            };
            
            Container.Bind(typeof(IDataSectionRegistry<string>)).To<DataSectionRegistry>().AsSingle().WithArguments(dataSectionBindings);
            Container.Bind(typeof(IDataService), typeof(IDisposable)).To<PlayerPrefsDataService>().AsSingle();
        }

        private void InstallFactory()
        {
            Container.Bind<WindowRoot>().FromInstance(_windowsRoot.GetComponent<WindowRoot>()).AsSingle();
            Container.Bind(typeof(IViewFactory)).To<ViewFactory>().AsSingle().WithArguments(_windowsRoot, WINDOW_PREFAB_PATH);
        }

        private void InstallAudio()
        {
            Container.BindInterfacesAndSelfTo<AudioService>().AsSingle().NonLazy();
            Container.BindInterfacesTo<UIAudioController>().AsSingle().NonLazy();
        }

        private void InstallDifficulty()
        {
            Container.Bind(typeof(ISelectedDifficultyService), typeof(IDifficultyService))
                .To<SelectedDifficultyService>()
                .AsSingle();
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
            Container.Bind<IInputGate>().To<InputGate>().AsSingle();
            Container.BindInterfacesTo<InputController>().AsSingle().NonLazy();
        }
        
        private void InstallConfigs()
        {
            Container.Bind(typeof(OrbitConfig)).To<OrbitConfig>().FromScriptableObjectResource("Configs/OrbitConfig").AsSingle();
            Container.Bind(typeof(LevelGenerationConfig)).To<LevelGenerationConfig>().FromScriptableObjectResource("Configs/LevelGenerationConfig").AsSingle();
            Container.Bind<ObstaclePatternGenerationCatalog>().FromScriptableObjectResource("Configs/ObstaclePatternGenerationCatalog").AsSingle();
            Container.Bind(typeof(GamePaletteConfig)).To<GamePaletteConfig>().FromScriptableObjectResource("Configs/GamePaletteConfig").AsSingle();
            Container.Bind<MainMenuPaletteConfig>().FromScriptableObjectResource("Configs/MainMenuPaletteConfig").AsSingle();
            Container.Bind<PaletteFontMaterialConfig>().FromScriptableObjectResource("Configs/PaletteFontMaterialConfig").AsSingle();
            Container.Bind(typeof(MovementSpeedConfig)).To<MovementSpeedConfig>().FromScriptableObjectResource("Configs/MovementSpeedConfig").AsSingle();
            Container.Bind<CameraSettingsConfig>().FromScriptableObjectResource("Configs/CameraSettingsConfig").AsSingle();
            Container.Bind(typeof(RiftConfig)).To<RiftConfig>().FromScriptableObjectResource("Configs/RiftConfig").AsSingle();
            Container.Bind(typeof(SpikeConfig)).To<SpikeConfig>().FromScriptableObjectResource("Configs/SpikeConfig").AsSingle();
            Container.Bind<SpikePresentationConfig>().FromScriptableObjectResource("Configs/SpikePresentationConfig").AsSingle();
            Container.Bind(typeof(MiniSpikeConfig)).To<MiniSpikeConfig>().FromScriptableObjectResource("Configs/MiniSpikeConfig").AsSingle();
            Container.Bind(typeof(ImpulseTrapConfig)).To<ImpulseTrapConfig>().FromScriptableObjectResource("Configs/ImpulseTrapConfig").AsSingle();
            Container.Bind(typeof(GravityWellConfig)).To<GravityWellConfig>().FromScriptableObjectResource("Configs/GravityWellConfig").AsSingle();
            Container.Bind(typeof(LaserGateConfig)).To<LaserGateConfig>().FromScriptableObjectResource("Configs/LaserGateConfig").AsSingle();
            Container.Bind(typeof(GameAudioConfig)).To<GameAudioConfig>().FromScriptableObjectResource("Configs/GameAudioConfig").AsSingle();
            Container.Bind(typeof(DifficultySelectionConfig)).To<DifficultySelectionConfig>().FromScriptableObjectResource("Configs/DifficultySelectionConfig").AsSingle();
            Container.Bind(typeof(CheatPanelConfig)).To<CheatPanelConfig>().FromScriptableObjectResource("Configs/CheatPanelConfig").AsSingle();
            Container.Bind(typeof(ObjectPoolConfig)).To<ObjectPoolConfig>().FromScriptableObjectResource("Configs/ObjectPoolConfig").AsSingle();
        }
    }
}
