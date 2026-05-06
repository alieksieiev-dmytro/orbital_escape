using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Factories;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class LevelObjectSpawner : ILevelObjectSpawner
    {
        private readonly IObjectFactory<GameObject> _orbitFactory;
        private readonly List<IOrbitObjectSpawner> _objectSpawners;

        [Inject]
        public LevelObjectSpawner(
            [Inject(Id = FactoryType.OrbitFactory)] IObjectFactory<GameObject> orbitFactory,
            List<IOrbitObjectSpawner> objectSpawners)
        {
            _orbitFactory = orbitFactory;
            _objectSpawners = objectSpawners
                .OrderBy(spawner => spawner.Order)
                .ToList();
        }

        public GameObject SpawnOrbit(Orbit orbit)
        {
            GameObject orbitObject = _orbitFactory.Create(orbit);

            foreach (IOrbitObjectSpawner spawner in _objectSpawners)
            {
                spawner.Spawn(orbitObject, orbit);
            }

            return orbitObject;
        }

        public GameObject GetOrbitObject(int index)
        {
            return _orbitFactory.Get(index);
        }

        public void Clear()
        {
            _orbitFactory.Clear();

            foreach (IOrbitObjectSpawner spawner in _objectSpawners)
            {
                spawner.Clear();
            }
        }
    }
}
