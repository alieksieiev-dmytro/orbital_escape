using System;
using OrbitalEscape.Audio;
using OrbitalEscape.Input;
using Zenject;

namespace OrbitalEscape.UI
{
    public class RestartWindowController : BaseController<RestartWindowView>
    {
        private readonly IViewFactory _viewFactory;
        private readonly Command _restartCommand;
        private readonly IAudioService _audioService;
        private readonly IUIManager _uiManager;
        
        public override IConvertible Type => WindowType.Restart;

        [Inject]
        public RestartWindowController(IViewFactory viewFactory,
            [Inject(Id = nameof(CommandType.Restart))] Command restartCommand,
            IAudioService audioService, IUIManager uiManager)
        {
            _viewFactory = viewFactory;
            _restartCommand = restartCommand;
            _audioService = audioService;
            _uiManager = uiManager;
        }

        public override void Initialize(ScreenData screenData)
        {
            _audioService.SetMusicObscured(true);
            View.RestartClicked += OnRestartClicked;
            View.Show();
        }
        
        public override void Complete()
        {
            _audioService.SetMusicObscured(false);
            if (IsNullView)
                return;

            View.RestartClicked -= OnRestartClicked;
            View.Hide();
        }

        public override void RequestClose() { }

        private void OnRestartClicked()
        {
            _uiManager.CloseWindow(Type);
            _restartCommand.Execute();
        }
        
        protected override RestartWindowView CreateView() => _viewFactory.Create<RestartWindowView>(nameof(WindowType.Restart));
    }
}
