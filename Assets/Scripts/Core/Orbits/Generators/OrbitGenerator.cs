using System.Collections.Generic;
using OrbitalEscape.Orbits.Generators;

namespace OrbitalEscape.Orbits
{
    public class OrbitGenerator
    {
        public List<Orbit> GenerateOrbits(
            int startIndex,
            int count,
            OrbitGenerationSettings settings)
        {
            List<Orbit> orbits = new List<Orbit>(count);
            
            for (int i = 0; i < count; i++)
            {
                int orbitIndex = startIndex + i;
                float orbitRadius = settings.FirstOrbitRadius + orbitIndex * settings.OrbitRadiusStep;
                var newOrbit = new Orbit(orbitRadius, orbitIndex);
                orbits.Add(newOrbit);
            }
            
            return orbits;
        }
    }
}
