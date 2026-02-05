using System;
using UnityEngine.SceneManagement;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class RestartCommand : Command
    {
        public override IConvertible Type => CommandType.Restart;

        private readonly string _sceneName;
        private readonly ZenjectSceneLoader _zenjectSceneLoader;

        [Inject]
        public RestartCommand(ZenjectSceneLoader zenjectSceneLoader, string sceneName)
        {
            _zenjectSceneLoader = zenjectSceneLoader;
            _sceneName = sceneName;
        }
        
        public override void Execute()
        {
            _zenjectSceneLoader.LoadScene(_sceneName);
        }
    }
}
