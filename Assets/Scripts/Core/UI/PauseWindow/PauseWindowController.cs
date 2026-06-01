using System;
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

        public override IConvertible Type => WindowType.Pause;

        [Inject]
        public PauseWindowController(IViewFactory viewFactory,
            [Inject(Id = nameof(CommandType.Resume))] Command resumeCommand,
            [Inject(Id = nameof(CommandType.Restart))] Command restartCommand,
            [Inject(Id = nameof(CommandType.ExitToMainMenu))] Command exitToMainMenuCommand)
        {
            _viewFactory = viewFactory;
            _resumeCommand = resumeCommand;
            _restartCommand = restartCommand;
            _exitToMainMenuCommand = exitToMainMenuCommand;
        }

        public override void Initialize(ScreenData screenData)
        {
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
