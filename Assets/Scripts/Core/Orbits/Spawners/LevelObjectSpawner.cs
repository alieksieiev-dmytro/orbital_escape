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
        private readonly IObjectFactory<GameObject> _orbitFactory;
        private readonly List<IOrbitObjectSpawner> _objectSpawners;
        private readonly IPaletteService _paletteService;
        private readonly IPaletteObjectPainter _paletteObjectPainter;
        private readonly List<GameObject> _spawnedOrbitObjects;

        public IReadOnlyList<GameObject> SpawnedOrbitObjects => _spawnedOrbitObjects;

        [Inject]
        public LevelObjectSpawner(
            [Inject(Id = FactoryType.OrbitFactory)] IObjectFactory<GameObject> orbitFactory,
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
            GameObject orbitObject = _orbitFactory.Create(orbit);

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

        public GameObject GetOrbitObject(int index)
        {
            return _orbitFactory.Get(index);
        }

        public void Clear()
        {
            _orbitFactory.Clear();
            _spawnedOrbitObjects.Clear();

            foreach (IOrbitObjectSpawner spawner in _objectSpawners)
            {
                spawner.Clear();
            }
        }
    }
}
