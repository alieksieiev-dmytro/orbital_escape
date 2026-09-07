using System;
using OrbitalEscape.Audio;
using OrbitalEscape.Cheats;
using OrbitalEscape.Input;
using Zenject;

namespace OrbitalEscape.UI
{
    public class PauseWindowController : BaseController<PauseWindowView>
    {
        private readonly IViewFactory _viewFactory;
        private readonly Command _resumeCommand;
        private readonly Command _restartCommand;
        private readonly Command _exitToMainMenuCommand;
        private readonly IAudioService _audioService;
        private readonly IUIManager _uiManager;
        private readonly CheatPanelConfig _cheatPanelConfig;

        public override IConvertible Type => WindowType.Pause;

        [Inject]
        public PauseWindowController(IViewFactory viewFactory,
            [Inject(Id = nameof(CommandType.Resume))] Command resumeCommand,
            [Inject(Id = nameof(CommandType.Restart))] Command restartCommand,
            [Inject(Id = nameof(CommandType.ExitToMainMenu))] Command exitToMainMenuCommand,
            IAudioService audioService,
            IUIManager uiManager,
            CheatPanelConfig cheatPanelConfig)
        {
            _viewFactory = viewFactory;
            _resumeCommand = resumeCommand;
            _restartCommand = restartCommand;
            _exitToMainMenuCommand = exitToMainMenuCommand;
            _audioService = audioService;
            _uiManager = uiManager;
            _cheatPanelConfig = cheatPanelConfig;
        }

        public override void Initialize(ScreenData screenData)
        {
            _audioService.SetMusicObscured(true);
            View.ResumeClicked += OnResumeClicked;
            View.RestartClicked += OnRestartClicked;
            View.ExitClicked += OnExitClicked;
            View.CheatsClicked += OnCheatsClicked;
            View.SetCheatsVisible(_cheatPanelConfig.ShouldShowCommandPanel);
            View.Show();
        }

        public override void Complete()
        {
            _audioService.SetMusicObscured(false);
            if (IsNullView)
                return;

            View.ResumeClicked -= OnResumeClicked;
            View.RestartClicked -= OnRestartClicked;
            View.ExitClicked -= OnExitClicked;
            View.CheatsClicked -= OnCheatsClicked;
            View.Hide();
        }

        protected override PauseWindowView CreateView() => _viewFactory.Create<PauseWindowView>(nameof(WindowType.Pause));

        private void OnResumeClicked()
        {
            _resumeCommand.Execute();
        }

        public override void RequestClose() => OnResumeClicked();

        private void OnRestartClicked()
        {
            _restartCommand.Execute();
        }

        private void OnExitClicked()
        {
            _uiManager.CloseWindow(Type);
            _exitToMainMenuCommand.Execute();
        }

        private void OnCheatsClicked()
        {
            if (!_cheatPanelConfig.ShouldShowCommandPanel)
            {
                return;
            }

            _uiManager.CloseWindow(Type);
            _uiManager.OpenWindow(WindowType.CheatCommand, null);
        }
    }
}
