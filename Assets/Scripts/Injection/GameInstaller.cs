using System;
using OrbitalEscape.Factories;
using OrbitalEscape.Input;
using OrbitalEscape.Movement;
using OrbitalEscape.Orbits;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Score;
using UnityEngine;
using Zenject;
using static OrbitalEscape.Constants;

namespace OrbitalEscape.Injection
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private Transform _player;
        
        [SerializeField] private Transform _orbitRoot;
        [SerializeField] private GameObject _orbitPrefab;
        [SerializeField] private GameObject _spikePrefab;
        
        private PlayerMovementController _movementController;

        public override void InstallBindings()
        {
            BindGenerators();
            BindFactories();
            InstallCommands();
            
            Container.Bind<Vector2>().WithId(INJECTION_ID_CENTRE).FromInstance(Center);
            
            Container.Bind<IMovementModel>().To<CircularOrbitModel>().FromInstance(new CircularOrbitModel(0.0f, 15f + CHARACTER_OFFSET_FROM_ORBIT)).AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerMovementController>().AsSingle().WithArguments(_player, 5.0f);
            Container.Bind<LevelManager>().FromComponentInHierarchy().AsSingle();

            Container.Bind(typeof(JumpBehaviour), typeof(IDisposable)).To<JumpBehaviour>().AsSingle().NonLazy();
            Container.Bind(typeof(PlayerController), typeof(IDisposable)).To<PlayerController>().AsSingle().WithArguments(_player).NonLazy();
            Container.Bind(typeof(IScoreService), typeof(IDisposable)).To<ScoreService>().AsSingle().NonLazy();
        }
        
        private void BindGenerators()
        {
            Container.Bind(typeof(OrbitGenerator)).To<OrbitGenerator>().AsSingle();
            Container.Bind<IArcGenerator>().WithId(ArcObjectType.Rift).To<RiftGenerator>().AsSingle();
            Container.Bind<IArcGenerator>().WithId(ArcObjectType.Spike).To<SpikeGenerator>().AsSingle();
        }
        
        private void BindFactories()
        {
            Container.Bind(typeof(IObjectFactory<GameObject>)).WithId(FactoryType.OrbitFactory).To<OrbitFactory>().AsSingle().WithArguments(_orbitPrefab, _orbitRoot);
            Container.Bind(typeof(IObjectFactory<GameObject>)).WithId(FactoryType.SpikeFactory).To<SpikeFactory>().AsSingle().WithArguments(_spikePrefab);
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
