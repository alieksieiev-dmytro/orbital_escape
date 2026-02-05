using System;
using OrbitalEscape.Input;
using OrbitalEscape.Movement;
using OrbitalEscape.Orbits;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Score;
using OrbitalEscape.UI;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Injection
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private Transform _player;
        [SerializeField] private Vector2 _centre;
        
        private PlayerMovementController _movementController;

        public override void InstallBindings()
        {
            BindConfigs();
            BindGenerators();
            InstallCommands();
            
            Container.Bind<Vector2>().WithId(Constants.INJECTION_ID_CENTRE).FromInstance(_centre);
            
            Container.Bind<IMovementModel>().To<CircularOrbitModel>()
                .FromInstance(new CircularOrbitModel(0.0f, 15f + Constants.CHARACTER_OFFSET_FROM_ORBIT))
                .AsSingle();
            
            Container.BindInterfacesAndSelfTo<PlayerMovementController>()
                .AsSingle()
                .WithArguments(_player, 5.0f);
            
            Container.Bind<CollisionDetector>()
                .AsSingle()
                .WithArguments(_centre)
                .NonLazy();

            Container.Bind<LevelManager>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind(typeof(JumpBehaviour), typeof(IDisposable)).To<JumpBehaviour>().AsSingle().WithArguments(_levelManager.GetOrbits()[0], _levelManager.GetOrbits()[1]);
            Container.Bind(typeof(PlayerController), typeof(IDisposable)).To<PlayerController>().AsSingle().WithArguments(_player).NonLazy();
            Container.Bind(typeof(IScoreService), typeof(IDisposable)).To<ScoreService>().AsSingle().NonLazy();
        }
        
        private void BindConfigs()
        {
            Container.Bind<RiftConfig>()
                .FromScriptableObjectResource("Configs/RiftConfig")
                .AsSingle();
            Container.Bind<SpikeConfig>()
                .FromScriptableObjectResource("Configs/SpikeConfig")
                .AsSingle();
        }
        
        private void BindGenerators()
        {
            Container.Bind<IArcGenerator>()
                .WithId(Constants.INJECTION_ID_RIFT_GENERATOR)
                .To<RiftGenerator>()
                .AsSingle();
            Container.Bind<IArcGenerator>()
                .WithId(Constants.INJECTION_ID_SPIKE_GENERATOR)
                .To<SpikeGenerator>()
                .AsSingle();
        }

        private void InstallCommands()
        {
            InstallCommand<RestartCommand>(nameof(CommandType.Restart), Constants.SCENE_GAMEPLAY);
            InstallCommand<JumpCommand>(nameof(CommandType.Jump));
            InstallCommand<MoveClockwiseCommand>(nameof(CommandType.MoveClockwise));
            InstallCommand<MoveCounterClockwiseCommand>(nameof(CommandType.MoveCounterClockwise));
        }

        private void InstallCommand<TCommand>(string id, params object[] args) where TCommand : Command
        {
            Container.BindInterfacesAndSelfTo<TCommand>().AsSingle().WithArguments(args).NonLazy();
            Container.Bind<Command>().WithId(id).FromResolveGetter<TCommand>(x => x);
        }
    }
}
