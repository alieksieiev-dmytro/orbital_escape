using System;
using OrbitalEscape.Audio;
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

        public override IConvertible Type => WindowType.Pause;

        [Inject]
        public PauseWindowController(IViewFactory viewFactory,
            [Inject(Id = nameof(CommandType.Resume))] Command resumeCommand,
            [Inject(Id = nameof(CommandType.Restart))] Command restartCommand,
            [Inject(Id = nameof(CommandType.ExitToMainMenu))] Command exitToMainMenuCommand,
            IAudioService audioService)
        {
            _viewFactory = viewFactory;
            _resumeCommand = resumeCommand;
            _restartCommand = restartCommand;
            _exitToMainMenuCommand = exitToMainMenuCommand;
            _audioService = audioService;
        }

        public override void Initialize(ScreenData screenData)
        {
            _audioService.SetMusicObscured(true);
            View.ResumeClicked += OnResumeClicked;
            View.RestartClicked += OnRestartClicked;
            View.ExitClicked += OnExitClicked;
            View.Show();
        }

        public override void Complete()
        {
            View.ResumeClicked -= OnResumeClicked;
            View.RestartClicked -= OnRestartClicked;
            View.ExitClicked -= OnExitClicked;
            View.Hide();
            _audioService.SetMusicObscured(false);
        }

        protected override PauseWindowView CreateView() => _viewFactory.Create<PauseWindowView>(nameof(WindowType.Pause));

        private void OnResumeClicked()
        {
            Complete();
            _resumeCommand.Execute();
        }

        private void OnRestartClicked()
        {
            Complete();
            _resumeCommand.Execute();
            _restartCommand.Execute();
        }

        private void OnExitClicked()
        {
            Complete();
            _exitToMainMenuCommand.Execute();
        }
    }
}
