using System;
using System.Collections.Generic;
using OrbitalEscape.Data;
using OrbitalEscape.Input;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.UI;
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
            DontDestroyOnLoad(_windowsRoot.gameObject);

            InstallConfigs();
            InstallData();
            InstallFactory();
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
            Container.Bind(typeof(IViewFactory)).To<ViewFactory>().AsSingle().WithArguments(_windowsRoot, WINDOW_PREFAB_PATH);
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
        
        private void InstallConfigs()
        {
            Container.Bind(typeof(RiftConfig)).To<RiftConfig>().FromScriptableObjectResource("Configs/RiftConfig").AsSingle();
            Container.Bind(typeof(SpikeConfig)).To<SpikeConfig>().FromScriptableObjectResource("Configs/SpikeConfig").AsSingle();
        }
    }
}
