using System.Collections.Generic;
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

        public void Spawn(GameObject orbitObject, Orbit orbit, IReadOnlyList<ArcObject> objects)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                ArcObject spike = objects[i];
                if (spike.Object.Type != ArcObjectType.Spike)
                {
                    continue;
                }

                _spikeFactory.Create(orbitObject.transform, orbit, spike);
            }
        }

        public void Clear()
        {
            _spikeFactory.Clear();
        }
    }
}
