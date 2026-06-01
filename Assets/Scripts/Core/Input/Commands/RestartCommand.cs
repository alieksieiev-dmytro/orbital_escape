using System;
using OrbitalEscape.Pause;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class RestartCommand : Command
    {
        private readonly string _sceneName;
        private readonly IGamePauseService _pauseService;
        private readonly ZenjectSceneLoader _zenjectSceneLoader;

        public override IConvertible Type => CommandType.Restart;

        [Inject]
        public RestartCommand(ZenjectSceneLoader zenjectSceneLoader, IGamePauseService pauseService, string sceneName)
        {
            _zenjectSceneLoader = zenjectSceneLoader;
            _pauseService = pauseService;
            _sceneName = sceneName;
        }
        
        public override void Execute()
        {
            if (_pauseService.IsPaused)
            {
                return;
            }

            _zenjectSceneLoader.LoadScene(_sceneName);
        }
    }
}
