using System;
using OrbitalEscape.Pause;
using OrbitalEscape.UI;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class ResumeCommand : Command
    {
        private readonly IGamePauseService _pauseService;
        private readonly IUIManager _uiManager;

        public override IConvertible Type => CommandType.Resume;

        [Inject]
        public ResumeCommand(IGamePauseService pauseService, IUIManager uiManager)
        {
            _pauseService = pauseService;
            _uiManager = uiManager;
        }

        public override void Execute()
        {
            _pauseService.Resume();
            _uiManager.CloseWindow(WindowType.Pause);
        }
    }
}
