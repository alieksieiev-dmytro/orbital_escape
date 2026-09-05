using System;
using OrbitalEscape.Input;
using Zenject;

namespace OrbitalEscape.UI
{
    public class MainMenuWindowController : BaseController<MainMenuWindowView>
    {
        private readonly IViewFactory _viewFactory;
        private readonly IUIManager _uiManager;
        private readonly Command _exitCommand;
        
        public override IConvertible Type => WindowType.MainMenu;

        [Inject]
        public MainMenuWindowController(IViewFactory viewFactory,
            IUIManager uiManager,
            [Inject(Id = nameof(CommandType.Exit))] Command exitCommand)
        {
            _viewFactory = viewFactory;
            _uiManager = uiManager;
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
            if (IsNullView)
                return;

            View.PlayClicked -= OnPlayClicked;
            View.SettingsClicked -= OnSettingsClicked;
            View.ExitClicked -= OnExitClicked;
            View.Hide();
        }

        private void OnPlayClicked()
        {
            _uiManager.CloseWindow(Type);
            _uiManager.OpenWindow(WindowType.SelectGameMode, null);
        }

        private void OnSettingsClicked()
        {
            throw new NotImplementedException("Settings window is not implemented yet.");
        }

        private void OnExitClicked()
        {
            _uiManager.CloseWindow(Type);
            _exitCommand.Execute();
        }

        public override void RequestClose() { }

        protected override MainMenuWindowView CreateView() => _viewFactory.Create<MainMenuWindowView>(nameof(WindowType.MainMenu));
    }
}
