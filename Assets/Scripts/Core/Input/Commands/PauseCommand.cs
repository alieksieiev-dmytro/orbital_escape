using System;
using OrbitalEscape.Pause;
using OrbitalEscape.UI;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class PauseCommand : Command
    {
        private readonly IGamePauseService _pauseService;
        private readonly IUIManager _uiManager;
        
        public override IConvertible Type => CommandType.Pause;

        [Inject]
        public PauseCommand(IGamePauseService pauseService, IUIManager uiManager)
        {
            _pauseService = pauseService;
            _uiManager = uiManager;
        }

        public override void Execute()
        {
            if (_pauseService.IsPaused)
            {
                _pauseService.Resume();
                _uiManager.CloseWindow(WindowType.Pause);
                return;
            }

            _pauseService.Pause();
            _uiManager.OpenWindow(WindowType.Pause, null);
        }
    }
}
