using System;
using Cysharp.Threading.Tasks;
using OrbitalEscape.Restart;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class RestartCommand : Command
    {
        private readonly IGameRestartService _restartService;

        public override IConvertible Type => CommandType.Restart;

        [Inject]
        public RestartCommand(IGameRestartService restartService)
        {
            _restartService = restartService;
        }
        
        public override void Execute()
        {
            _restartService.RestartAsync().Forget();
        }
    }
}
