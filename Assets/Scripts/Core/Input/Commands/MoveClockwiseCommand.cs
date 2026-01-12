using OrbitalEscape.Movement;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class MoveClockwiseCommand : ICommand
    {
        private readonly PlayerMovementController _playerMovementController;
        
        [Inject]
        public MoveClockwiseCommand(PlayerMovementController playerMovementController)
        {
            _playerMovementController = playerMovementController;
        }
        
        public void Execute()
        {
            _playerMovementController.SetMovementDirection(MovementDirection.Clockwise);
        }
    }
}