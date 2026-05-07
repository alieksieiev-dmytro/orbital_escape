using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace OrbitalEscape.Orbits.Generators
{
    public interface ILevelBuilder
    {
        UniTask<LevelBuildResult> BuildAsync(
            IReadOnlyList<Orbit> existingOrbits,
            int count,
            CancellationToken cancellationToken);
    }
}
