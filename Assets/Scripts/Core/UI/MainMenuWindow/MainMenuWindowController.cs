using System;
using OrbitalEscape.Input;
using Zenject;

namespace OrbitalEscape.UI
{
    public class MainMenuWindowController : BaseController<MainMenuWindowView>
    {
        private readonly IViewFactory _viewFactory;
        private readonly Command _playCommand;
        private readonly Command _exitCommand;
        
        public override IConvertible Type => WindowType.MainMenu;

        [Inject]
        public MainMenuWindowController(IViewFactory viewFactory,
            [Inject(Id = nameof(CommandType.Play))] Command playCommand,
            [Inject(Id = nameof(CommandType.Exit))] Command exitCommand)
        {
            _viewFactory = viewFactory;
            _playCommand = playCommand;
            _exitCommand = exitCommand;
        }

        public override void Initialize(ScreenData screenData)
        {
            View.PlayClicked += OnPlayClicked;
            View.SettingsClicked += OnSettingsClicked;
            View.ExitClicked += OnExitClicked;
            View.Show();
        }

        public override void Complete()
        {
            View.PlayClicked -= OnPlayClicked;
            View.SettingsClicked -= OnSettingsClicked;
            View.ExitClicked -= OnExitClicked;
            View.Hide();
        }

        private void OnPlayClicked()
        {
            Complete();
            _playCommand.Execute();
        }

        private void OnSettingsClicked()
        {
            throw new NotImplementedException("Settings window is not implemented yet.");
        }

        private void OnExitClicked()
        {
            _exitCommand.Execute();
        }

        protected override MainMenuWindowView CreateView() => _viewFactory.Create<MainMenuWindowView>(nameof(WindowType.MainMenu));
    }
}
