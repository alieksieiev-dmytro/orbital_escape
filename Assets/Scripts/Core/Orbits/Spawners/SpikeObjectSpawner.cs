using OrbitalEscape.Factories;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class SpikeObjectSpawner : IOrbitObjectSpawner
    {
        private readonly IObjectFactory<GameObject> _spikeFactory;

        public int Order => 10;

        [Inject]
        public SpikeObjectSpawner([Inject(Id = FactoryType.SpikeFactory)] IObjectFactory<GameObject> spikeFactory)
        {
            _spikeFactory = spikeFactory;
        }

        public void Spawn(GameObject orbitObject, Orbit orbit)
        {
            foreach (ArcObject spike in orbit.GetOrbitObjectsByType(ArcObjectType.Spike))
            {
                _spikeFactory.Create(orbitObject.transform, orbit, spike);
            }
        }

        public void Clear()
        {
            _spikeFactory.Clear();
        }
    }
}
