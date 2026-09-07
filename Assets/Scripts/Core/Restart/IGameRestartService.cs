using Cysharp.Threading.Tasks;

namespace OrbitalEscape.Restart
{
    public interface IGameRestartService
    {
        bool IsRestarting { get; }
        UniTask RestartAsync();
    }
}
