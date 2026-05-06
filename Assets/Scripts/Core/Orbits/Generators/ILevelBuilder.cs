using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators
{
    public interface ILevelBuilder
    {
        List<Orbit> Build(IReadOnlyList<Orbit> existingOrbits, int count);
    }
}
