using OrbitalEscape.Movement;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class MoveCounterClockwiseCommand : ICommand
    {
        private readonly PlayerMovementController _playerMovementController;

        [Inject]
        public MoveCounterClockwiseCommand(PlayerMovementController playerMovementController) =>
            _playerMovementController = playerMovementController;
        
        public void Execute() => _playerMovementController.SetMovementDirection(MovementDirection.CounterClockwise);
    }
}