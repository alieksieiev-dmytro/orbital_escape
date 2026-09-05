using System;
using OrbitalEscape.Movement;
using OrbitalEscape.Pause;
using OrbitalEscape.UI;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class PauseCommand : Command
    {
        private readonly IGamePauseService _pauseService;
        private readonly IUIManager _uiManager;
        private readonly IPlayerLifeState _lifeState;
        
        public override IConvertible Type => CommandType.Pause;

        [Inject]
        public PauseCommand(IGamePauseService pauseService, IUIManager uiManager,
            IPlayerLifeState lifeState)
        {
            _pauseService = pauseService;
            _uiManager = uiManager;
            _lifeState = lifeState;
        }

        public override void Execute()
        {
            if (!_lifeState.IsAlive || _uiManager.HasOpenWindow)
            {
                return;
            }

            if (_pauseService.IsPaused)
            {
                _uiManager.CloseWindow(WindowType.Pause);
                _pauseService.Resume();
                return;
            }

            _pauseService.Pause();
            _uiManager.OpenWindow(WindowType.Pause, null);
        }
    }
}
