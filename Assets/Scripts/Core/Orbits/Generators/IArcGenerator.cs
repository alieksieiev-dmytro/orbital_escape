using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators
{
    public interface IArcGenerator
    {
        List<ArcObject> GenerateArcs(Orbit orbit, ArcType arcType, LevelBuildContext context);
    }
}
