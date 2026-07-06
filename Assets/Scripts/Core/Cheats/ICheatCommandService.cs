using System.Threading;
using Cysharp.Threading.Tasks;

namespace OrbitalEscape.Cheats
{
    public interface ICheatCommandService
    {
        CheatCommandResult AddScore();
        CheatCommandResult ClearAllObstacles();
        UniTask<CheatCommandResult> GoToOrbitAsync(int orbitIndex, CancellationToken cancellationToken);
    }
}
