using Zenject;

namespace OrbitalEscape.UI
{
    public class MainMenuStartupController : IInitializable
    {
        private readonly IUIManager _uiManager;

        [Inject]
        public MainMenuStartupController(IUIManager uiManager)
        {
            _uiManager = uiManager;
        }

        public void Initialize()
        {
            _uiManager.OpenWindow(WindowType.MainMenu, null);
        }
    }
}
