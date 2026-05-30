using System;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class PlayCommand : Command
    {
        private readonly string _sceneName;
        private readonly ZenjectSceneLoader _zenjectSceneLoader;
        
        public override IConvertible Type => CommandType.Play;

        [Inject]
        public PlayCommand(ZenjectSceneLoader zenjectSceneLoader, string sceneName)
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
