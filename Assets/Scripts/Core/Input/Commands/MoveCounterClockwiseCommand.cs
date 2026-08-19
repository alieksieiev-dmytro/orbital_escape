using System;
using OrbitalEscape.Movement;
using OrbitalEscape.Pause;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class MoveCounterClockwiseCommand : Command
    {
        private readonly PlayerMovementController _playerMovementController;
        private readonly IGamePauseService _pauseService;
        private readonly IPlayerLifeState _lifeState;
        
        public override IConvertible Type => CommandType.MoveCounterClockwise;

        [Inject]
        public MoveCounterClockwiseCommand(PlayerMovementController playerMovementController,
            IGamePauseService pauseService, IPlayerLifeState lifeState)
        {
            _playerMovementController = playerMovementController;
            _pauseService = pauseService;
            _lifeState = lifeState;
        }
        
        public override void Execute()
        {
            if (_pauseService.IsPaused || !_lifeState.IsAlive)
            {
                return;
            }

            _playerMovementController.SetMovementDirection(MovementDirection.CounterClockwise);
        }
    }
}
