using System;
using OrbitalEscape.Input;
using Zenject;

namespace OrbitalEscape.UI
{
    public class RestartWindowController : BaseController<RestartWindowView>
    {
        private readonly IViewFactory _viewFactory;
        private readonly ICommand _restartCommand;
        
        public override IConvertible Type => WindowType.Restart;
        
        [Inject]
        public RestartWindowController(IViewFactory viewFactory, ICommand restartCommand)
        {
            _viewFactory = viewFactory;
            _restartCommand = restartCommand;
        }
        
        public override void Initialize(ScreenData screenData)
        {
            View.RestartClicked += OnRestartClicked;
            View.Show();
        }
        
        public override void Complete()
        {
            View.RestartClicked -= OnRestartClicked;
            View.Hide();
        }

        private void OnRestartClicked()
        {
            Complete();
            _restartCommand.Execute();
        }
        
        protected override RestartWindowView CreateView() => _viewFactory.Create<RestartWindowView>(nameof(WindowType.Restart));
    }
}
