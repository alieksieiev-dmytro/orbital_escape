using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Factories;
using OrbitalEscape.Visuals.Palettes;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class LevelObjectSpawner : ILevelObjectSpawner
    {
        private readonly IOrbitFactory _orbitFactory;
        private readonly List<IOrbitObjectSpawner> _objectSpawners;
        private readonly IPaletteService _paletteService;
        private readonly IPaletteObjectPainter _paletteObjectPainter;
        private readonly List<GameObject> _spawnedOrbitObjects;

        public IReadOnlyList<GameObject> SpawnedOrbitObjects => _spawnedOrbitObjects;

        [Inject]
        public LevelObjectSpawner(
            IOrbitFactory orbitFactory,
            List<IOrbitObjectSpawner> objectSpawners,
            IPaletteService paletteService,
            IPaletteObjectPainter paletteObjectPainter)
        {
            _orbitFactory = orbitFactory;
            _paletteService = paletteService;
            _paletteObjectPainter = paletteObjectPainter;
            _spawnedOrbitObjects = new List<GameObject>();
            _objectSpawners = objectSpawners
                .OrderBy(spawner => spawner.Order)
                .ToList();
        }

        public GameObject SpawnOrbit(Orbit orbit)
        {
            GameObject orbitObject = _orbitFactory.GetOrCreate(orbit);

            foreach (IOrbitObjectSpawner spawner in _objectSpawners)
            {
                spawner.Spawn(orbitObject, orbit, orbit.OrbitObjects);
            }

            _paletteObjectPainter.ApplyLevelObject(orbitObject, _paletteService.CurrentPalette);
            _spawnedOrbitObjects.Add(orbitObject);
            return orbitObject;
        }

        public void AppendOrbitContent(Orbit orbit, IReadOnlyList<ArcObject> addedObjects)
        {
            if (orbit == null || addedObjects == null || addedObjects.Count == 0)
            {
                return;
            }

            GameObject orbitObject = _orbitFactory.Get(orbit.Index);
            foreach (IOrbitObjectSpawner spawner in _objectSpawners)
            {
                spawner.Spawn(orbitObject, orbit, addedObjects);
            }

            _paletteObjectPainter.ApplyLevelObject(orbitObject, _paletteService.CurrentPalette);
        }

        public void RefreshOrbitContent(IReadOnlyList<Orbit> orbits)
        {
            foreach (IOrbitObjectSpawner spawner in _objectSpawners)
            {
                spawner.Clear(retainVisibility: true);
            }

            if (orbits == null)
            {
                return;
            }

            for (int i = 0; i < orbits.Count; i++)
            {
                Orbit orbit = orbits[i];
                if (orbit == null || orbit.Index < 0 || orbit.Index >= _spawnedOrbitObjects.Count)
                {
                    continue;
                }

                GameObject orbitObject = _orbitFactory.Get(orbit.Index);
                foreach (IOrbitObjectSpawner spawner in _objectSpawners)
                {
                    spawner.Spawn(orbitObject, orbit, orbit.OrbitObjects);
                }

                _paletteObjectPainter.ApplyLevelObject(orbitObject, _paletteService.CurrentPalette);
            }
        }

        public GameObject GetOrbitObject(int index)
        {
            return _orbitFactory.Get(index);
        }

        public void Clear()
        {
            foreach (IOrbitObjectSpawner spawner in _objectSpawners)
            {
                spawner.Clear();
            }

            _orbitFactory.DeactivateAll();
            _spawnedOrbitObjects.Clear();
        }
    }
}
