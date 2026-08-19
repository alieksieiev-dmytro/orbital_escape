using System;
using OrbitalEscape.Movement;
using OrbitalEscape.Pause;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class JumpCommand : Command
    {
        private readonly JumpBehaviour _jumpBehaviour;
        private readonly IGamePauseService _pauseService;
        private readonly IPlayerLifeState _lifeState;
        
        public override IConvertible Type => CommandType.Jump;
        
        [Inject]
        public JumpCommand(JumpBehaviour jumpBehaviour, IGamePauseService pauseService,
            IPlayerLifeState lifeState)
        {
            _jumpBehaviour = jumpBehaviour;
            _pauseService = pauseService;
            _lifeState = lifeState;
        }
        
        public override void Execute()
        {
            if (_pauseService.IsPaused || !_lifeState.IsAlive)
            {
                return;
            }

            _jumpBehaviour.Jump();
        }
    }
}
