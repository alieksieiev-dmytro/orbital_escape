using System.Collections.Generic;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public class OrbitGenerator
    {
        private readonly OrbitConfig _orbitConfig;
        private readonly List<Orbit> _orbits;
        
        public int Count => _orbits.Count;
        
        [Inject]
        public OrbitGenerator(OrbitConfig orbitConfig)
        {
            _orbitConfig = orbitConfig;
            _orbits = new List<Orbit>();
            GenerateOrbits(2);
        }

        public List<Orbit> GenerateOrbits(int count)
        {
            int currentCount = _orbits.Count;
            
            for (int i = 0; i < count; i++)
            {
                int orbitIndex = i + currentCount;
                float orbitRadius = _orbitConfig.FirstOrbitRadius + orbitIndex * _orbitConfig.OrbitRadiusStep;
                var newOrbit = new Orbit(orbitRadius, orbitIndex);
                _orbits.Add(newOrbit);
            }
            
            return _orbits;
        }
        
        public Orbit GetOrbit(int index) => _orbits[index];
    }
}
