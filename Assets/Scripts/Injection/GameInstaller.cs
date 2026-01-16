using OrbitalEscape.Input;
using OrbitalEscape.Movement;
using OrbitalEscape.Orbits;
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
            Container.Bind<Vector2>().WithId(Constants.INJECTION_ID_CENTRE).FromInstance(_centre);
            
            Container.Bind<IMovementModel>().To<CircularOrbitModel>()
                .FromInstance(new CircularOrbitModel(0.0f, 15f))
                .AsSingle();
            
            Container.BindInterfacesAndSelfTo<PlayerMovementController>()
                .AsSingle()
                .WithArguments(_player, 5.0f);
            
            Container.Bind<CollisionDetector>()
                .AsSingle()
                .WithArguments(_centre)
                .NonLazy();
            
            Container.Bind<JumpBehaviour>()
                .AsSingle()
                .WithArguments(_centre, _levelManager.GetOrbits()[0], _levelManager.GetOrbits()[1])
                .NonLazy();

            Container.Bind<LevelManager>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            InstallKeyBindings();
        }

        private void InstallKeyBindings()
        {
            Container.Bind<MoveClockwiseCommand>().AsTransient();
            Container.Bind<MoveCounterClockwiseCommand>().AsTransient();
            Container.Bind<JumpCommand>().AsTransient();

            Container.Bind<KeyBinding>().FromMethod(ctx =>
                new KeyBinding(nameof(KeyCode.D).ToLower(), ctx.Container.Resolve<MoveClockwiseCommand>())
            ).AsTransient();

            Container.Bind<KeyBinding>().FromMethod(ctx =>
                new KeyBinding(nameof(KeyCode.A).ToLower(), ctx.Container.Resolve<MoveCounterClockwiseCommand>())
            ).AsTransient();
            
            Container.Bind<KeyBinding>().FromMethod(ctx =>
                new KeyBinding(nameof(KeyCode.Space).ToLower(), ctx.Container.Resolve<JumpCommand>())
            ).AsTransient();


            Container.BindInterfacesTo<InputController>().AsSingle().NonLazy();
        }
    }
}
