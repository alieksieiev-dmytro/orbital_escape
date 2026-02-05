using System;
using OrbitalEscape.Movement;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class MoveClockwiseCommand : Command
    {
        public override IConvertible Type => CommandType.MoveClockwise;

        private readonly PlayerMovementController _playerMovementController;

        [Inject]
        public MoveClockwiseCommand(PlayerMovementController playerMovementController) =>
            _playerMovementController = playerMovementController;
        
        public override void Execute() => _playerMovementController.SetMovementDirection(MovementDirection.Clockwise);
    }
}