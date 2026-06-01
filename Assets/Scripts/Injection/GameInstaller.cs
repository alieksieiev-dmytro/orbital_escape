using System;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Factories;
using OrbitalEscape.Input;
using OrbitalEscape.Movement;
using OrbitalEscape.Orbits;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Pause;
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
            Container.BindInterfacesAndSelfTo<GamePauseService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PlayerMovementController>().AsSingle().WithArguments(_player, 5.0f);
            Container.Bind<LevelManager>().FromComponentInHierarchy().AsSingle();

            Container.Bind(typeof(JumpBehaviour), typeof(IDisposable)).To<JumpBehaviour>().AsSingle().NonLazy();
            Container.Bind(typeof(PlayerController), typeof(IDisposable)).To<PlayerController>().AsSingle().WithArguments(_player).NonLazy();
            Container.Bind(typeof(ImpulseTrapController), typeof(IDisposable)).To<ImpulseTrapController>().AsSingle().NonLazy();
            Container.Bind(typeof(IScoreService), typeof(IDisposable)).To<ScoreService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ScorePaletteService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PaletteSceneController>().AsSingle().NonLazy();
        }
        
        private void BindGenerators()
        {
            Container.Bind<OrbitGenerator>().AsSingle();
            Container.Bind<ILevelBuilder>().To<LevelBuilder>().AsSingle();
            Container.Bind<IOrbitContentPass>().To<RiftGenerationPass>().AsSingle();
            Container.Bind<IOrbitContentPass>().To<SpikeGenerationPass>().AsSingle();
            Container.Bind<IOrbitContentPass>().To<ImpulseTrapGenerationPass>().AsSingle();
            Container.Bind<IArcGenerator>().WithId(ArcObjectType.Rift).To<RiftGenerator>().AsSingle();
            Container.Bind<IArcGenerator>().WithId(ArcObjectType.Spike).To<SpikeGenerator>().AsSingle();
            Container.Bind<IArcGenerator>().WithId(ArcObjectType.ImpulseTrap).To<ImpulseTrapGenerator>().AsSingle();
        }
        
        private void BindFactories()
        {
            Container.Bind(typeof(IObjectFactory<GameObject>)).WithId(FactoryType.OrbitFactory).To<OrbitFactory>().AsSingle().WithArguments(_orbitPrefab, _orbitRoot);
            Container.Bind(typeof(IObjectFactory<RiftObject>)).WithId(FactoryType.RiftFactory).To<RiftFactory>().AsSingle().WithArguments(_riftObject);
            Container.Bind(typeof(IObjectFactory<GameObject>)).WithId(FactoryType.SpikeFactory).To<SpikeFactory>().AsSingle().WithArguments(_spikePrefab);
            Container.Bind(typeof(IObjectFactory<GameObject>)).WithId(FactoryType.ImpulseTrapFactory).To<ImpulseTrapFactory>().AsSingle().WithArguments(_impulseTrapPrefab);
            Container.Bind<ILevelObjectSpawner>().To<LevelObjectSpawner>().AsSingle();
            Container.Bind<IOrbitObjectSpawner>().To<RiftObjectSpawner>().AsSingle();
            Container.Bind<IOrbitObjectSpawner>().To<SpikeObjectSpawner>().AsSingle();
            Container.Bind<IOrbitObjectSpawner>().To<ImpulseTrapObjectSpawner>().AsSingle();
        }

        private void BindPalettes()
        {
            Container.Bind<IDifficultyService>().To<BasicDifficultyService>().AsSingle();
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
