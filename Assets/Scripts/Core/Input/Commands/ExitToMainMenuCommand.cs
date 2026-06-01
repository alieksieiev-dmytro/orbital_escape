using System;
using OrbitalEscape.Pause;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class ExitToMainMenuCommand : Command
    {
        private readonly IGamePauseService _pauseService;
        private readonly string _sceneName;
        private readonly ZenjectSceneLoader _zenjectSceneLoader;

        public override IConvertible Type => CommandType.ExitToMainMenu;

        [Inject]
        public ExitToMainMenuCommand(IGamePauseService pauseService, ZenjectSceneLoader zenjectSceneLoader, string sceneName)
        {
            _pauseService = pauseService;
            _zenjectSceneLoader = zenjectSceneLoader;
            _sceneName = sceneName;
        }

        public override void Execute()
        {
            _pauseService.Resume();
            _zenjectSceneLoader.LoadScene(_sceneName);
        }
    }
}
