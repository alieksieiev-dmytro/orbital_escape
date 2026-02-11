using System.Collections.Generic;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public class OrbitGenerator
    {
        private readonly OrbitConfig _orbitConfig;
        private readonly List<Orbit> _orbits;
        
        [Inject]
        public OrbitGenerator(OrbitConfig orbitConfig)
        {
            _orbitConfig = orbitConfig;
            _orbits = new List<Orbit>();
        }

        public List<Orbit> GenerateOrbits(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int orbitIndex = i + _orbits.Count;
                float orbitRadius = _orbitConfig.FirstOrbitRadius + orbitIndex * _orbitConfig.OrbitRadiusStep;
                var newOrbit = new Orbit(orbitRadius, orbitIndex);
                _orbits.Add(newOrbit);
            }
            
            return _orbits;
        }
    }
}
