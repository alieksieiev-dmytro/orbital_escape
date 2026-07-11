using System;
using OrbitalEscape.Audio;
using OrbitalEscape.Cheats;
using OrbitalEscape.Factories;
using OrbitalEscape.Input;
using OrbitalEscape.Movement;
using OrbitalEscape.Orbits;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Orbits.Generators.Patterns;
using OrbitalEscape.Pause;
using OrbitalEscape.Profiling;
using OrbitalEscape.Score;
using OrbitalEscape.Visuals.Palettes;
using UnityEngine;
using Zenject;
using static OrbitalEscape.Constants;

namespace OrbitalEscape.Injection
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private Transform _player;
        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private SpriteRenderer _accentRenderer;
        
        [SerializeField] private Transform _orbitRoot;
        [SerializeField] private GameObject _orbitPrefab;
        [SerializeField] private RiftObject _riftObject;
        [SerializeField] private GameObject _spikePrefab;
        [SerializeField] private GameObject _impulseTrapPrefab;
        [SerializeField] private GameObject _gravityWellPrefab;
        [SerializeField] private GameObject _laserGatePrefab;
        
        private PlayerMovementController _movementController;

        public override void InstallBindings()
        {
            BindGenerators();
            BindFactories();
            BindPalettes();
            InstallCommands();
            
            Container.Bind<Vector2>().WithId(INJECTION_ID_CENTRE).FromInstance(Center);
            
            Container.Bind<IMovementModel>().To<CircularOrbitModel>().FromInstance(new CircularOrbitModel(0.0f, 15f + CHARACTER_OFFSET_FROM_ORBIT)).AsSingle();
            Container.Bind<LevelState>().AsSingle();
            Container.Bind<IRiftClosureState>().To<RiftClosureState>().AsSingle();
            Container.BindInterfacesTo<RiftClosureService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<GamePauseService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PlayerOrbitState>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<OrbitJumpTransitionService>().AsSingle().NonLazy();
            Container.Bind<IJumpSpeedService>().To<ScoreJumpSpeedService>().AsSingle();
            Container.BindInterfacesAndSelfTo<GravityWellInfluenceService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PlayerMovementController>().AsSingle().WithArguments(_player);
            Container.Bind<LevelManager>().FromComponentInHierarchy().AsSingle();

            Container.Bind(typeof(JumpBehaviour), typeof(IDisposable)).To<JumpBehaviour>().AsSingle().NonLazy();
            Container.Bind(typeof(PlayerController), typeof(IDisposable)).To<PlayerController>().AsSingle().WithArguments(_player).NonLazy();
            Container.Bind(typeof(ImpulseTrapController), typeof(IDisposable)).To<ImpulseTrapController>().AsSingle().NonLazy();
            Container.Bind(typeof(IScoreService), typeof(IDisposable)).To<ScoreService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ScoreMovementSpeedService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ScorePaletteService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PaletteSceneController>().AsSingle().NonLazy();
            Container.BindInterfacesTo<GameplayAudioController>().AsSingle().NonLazy();
            Container.Bind<ICheatCommandService>().To<CheatCommandService>().AsSingle();
            Container.BindInterfacesAndSelfTo<ResourceProfilerService>().AsSingle().NonLazy();
        }
        
        private void BindGenerators()
        {
            Container.Bind<OrbitGenerator>().AsSingle();
            Container.Bind<ILevelBuilder>().To<LevelBuilder>().AsSingle();
            Container.Bind<WeightedPatternSelector>().AsSingle();
            Container.Bind<ILevelContentGenerator>().To<PatternLevelContentGenerator>().AsSingle();
        }
        
        private void BindFactories()
        {
            Container.Bind(typeof(IObjectFactory<GameObject>)).WithId(FactoryType.OrbitFactory).To<OrbitFactory>().AsSingle().WithArguments(_orbitPrefab, _orbitRoot);
            Container.Bind(typeof(IObjectFactory<RiftObject>)).WithId(FactoryType.RiftFactory).To<RiftFactory>().AsSingle().WithArguments(_riftObject);
            Container.BindInterfacesAndSelfTo<SpikeFactory>().AsSingle().WithArguments(_spikePrefab).NonLazy();
            Container.Bind(typeof(IObjectFactory<GameObject>)).WithId(FactoryType.ImpulseTrapFactory).To<ImpulseTrapFactory>().AsSingle().WithArguments(_impulseTrapPrefab);
            Container.Bind(typeof(IObjectFactory<GameObject>)).WithId(FactoryType.GravityWellFactory).To<GravityWellFactory>().AsSingle().WithArguments(_gravityWellPrefab);
            Container.Bind(typeof(IObjectFactory<GameObject>)).WithId(FactoryType.LaserGateFactory).To<LaserGateFactory>().AsSingle().WithArguments(_laserGatePrefab);
            Container.Bind<IGravityWellZoneRegistry>().To<GravityWellZoneRegistry>().AsSingle();
            Container.Bind<ILevelObjectSpawner>().To<LevelObjectSpawner>().AsSingle();
            Container.Bind<IOrbitObjectSpawner>().To<RiftObjectSpawner>().AsSingle();
            Container.BindInterfacesAndSelfTo<SpikeObjectSpawner>().AsSingle().NonLazy();
            Container.Bind<IOrbitObjectSpawner>().To<ImpulseTrapObjectSpawner>().AsSingle();
            Container.Bind<IOrbitObjectSpawner>().To<GravityWellObjectSpawner>().AsSingle();
            Container.Bind<IOrbitObjectSpawner>().To<LaserGateObjectSpawner>().AsSingle();
        }

        private void BindPalettes()
        {
            Container.Bind<PaletteSceneReferences>().FromInstance(new PaletteSceneReferences(_player, _backgroundRenderer, _accentRenderer)).AsSingle();
            Container.Bind<IPaletteObjectPainter>().To<PaletteObjectPainter>().AsSingle();
        }

        private void InstallCommands()
        {
            InstallCommand<RestartCommand>(nameof(CommandType.Restart), Constants.SCENE_GAMEPLAY);
            InstallCommand<PauseCommand>(nameof(CommandType.Pause));
            InstallCommand<ResumeCommand>(nameof(CommandType.Resume));
            InstallCommand<ExitToMainMenuCommand>(nameof(CommandType.ExitToMainMenu), Constants.SCENE_MAIN_MENU);
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
