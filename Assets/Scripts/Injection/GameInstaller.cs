using OrbitalEscape.Input;
using OrbitalEscape.Movement;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Injection
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private Transform _star;
        [SerializeField] private Transform _player;
        
        private PlayerMovementController _movementController;
        private InputController _inputController;
        
        public override void InstallBindings()
        {
            _movementController = new PlayerMovementController(_player, _star.position, 15.0f, 3.0f);
            
            Container.BindInterfacesAndSelfTo<PlayerMovementController>().FromInstance(_movementController).AsSingle();
            InstallKeyBindings();
        }

        private void InstallKeyBindings()
        {
            _inputController = new InputController();
            
            Container.Bind<MoveClockwiseCommand>().AsTransient();
            Container.Bind<MoveCounterClockwiseCommand>().AsTransient();

            Container.Bind<KeyBinding>().FromMethod(ctx =>
                new KeyBinding(nameof(KeyCode.D).ToLower(), ctx.Container.Resolve<MoveClockwiseCommand>())
            ).AsTransient();

            Container.Bind<KeyBinding>().FromMethod(ctx =>
                new KeyBinding(nameof(KeyCode.A).ToLower(), ctx.Container.Resolve<MoveCounterClockwiseCommand>())
            ).AsTransient();


            Container.BindInterfacesTo<InputController>().AsSingle().NonLazy();
        }
    }
}
