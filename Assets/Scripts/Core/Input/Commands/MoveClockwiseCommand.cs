using System;
using OrbitalEscape.Movement;
using OrbitalEscape.Pause;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class MoveClockwiseCommand : Command
    {
        private readonly PlayerMovementController _playerMovementController;
        private readonly IGamePauseService _pauseService;
        
        public override IConvertible Type => CommandType.MoveClockwise;

        [Inject]
        public MoveClockwiseCommand(PlayerMovementController playerMovementController, IGamePauseService pauseService)
        {
            _playerMovementController = playerMovementController;
            _pauseService = pauseService;
        }
        
        public override void Execute()
        {
            if (_pauseService.IsPaused)
            {
                return;
            }

            _playerMovementController.SetMovementDirection(MovementDirection.Clockwise);
        }
    }
}
