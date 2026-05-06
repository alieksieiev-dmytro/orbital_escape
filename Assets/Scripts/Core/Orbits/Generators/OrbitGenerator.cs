using System.Collections.Generic;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public class OrbitGenerator
    {
        private readonly OrbitConfig _orbitConfig;
        
        [Inject]
        public OrbitGenerator(OrbitConfig orbitConfig)
        {
            _orbitConfig = orbitConfig;
        }

        public List<Orbit> GenerateOrbits(int startIndex, int count)
        {
            List<Orbit> orbits = new List<Orbit>(count);
            
            for (int i = 0; i < count; i++)
            {
                int orbitIndex = startIndex + i;
                float orbitRadius = _orbitConfig.FirstOrbitRadius + orbitIndex * _orbitConfig.OrbitRadiusStep;
                var newOrbit = new Orbit(orbitRadius, orbitIndex);
                orbits.Add(newOrbit);
            }
            
            return orbits;
        }
    }
}
