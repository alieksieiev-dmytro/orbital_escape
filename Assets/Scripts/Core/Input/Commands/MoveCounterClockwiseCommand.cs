using System;
using OrbitalEscape.Movement;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class MoveCounterClockwiseCommand : Command
    {
        public override IConvertible Type => CommandType.MoveCounterClockwise;

        private readonly PlayerMovementController _playerMovementController;

        [Inject]
        public MoveCounterClockwiseCommand(PlayerMovementController playerMovementController) =>
            _playerMovementController = playerMovementController;
        
        public override void Execute() => _playerMovementController.SetMovementDirection(MovementDirection.CounterClockwise);
    }
}