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
        
        public override IConvertible Type => CommandType.Jump;
        
        [Inject]
        public JumpCommand(JumpBehaviour jumpBehaviour, IGamePauseService pauseService)
        {
            _jumpBehaviour = jumpBehaviour;
            _pauseService = pauseService;
        }
        
        public override void Execute()
        {
            if (_pauseService.IsPaused)
            {
                return;
            }

            _jumpBehaviour.Jump();
        }
    }
}
