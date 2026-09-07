using System.Collections.Generic;
using OrbitalEscape.Factories;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class GravityWellObjectSpawner : IOrbitObjectSpawner
    {
        private readonly IObjectFactory<GameObject> _gravityWellFactory;
        private readonly LevelState _levelState;
        private readonly IGravityWellZoneRegistry _zoneRegistry;
        private int _nextZoneId;

        public int Order => 30;

        [Inject]
        public GravityWellObjectSpawner(
            [Inject(Id = FactoryType.GravityWellFactory)] IObjectFactory<GameObject> gravityWellFactory,
            LevelState levelState,
            IGravityWellZoneRegistry zoneRegistry)
        {
            _gravityWellFactory = gravityWellFactory;
            _levelState = levelState;
            _zoneRegistry = zoneRegistry;
        }

        public void Spawn(GameObject orbitObject, Orbit orbit, IReadOnlyList<ArcObject> objects)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                ArcObject gravityWellData = objects[i];
                if (gravityWellData.Object.Type != ArcObjectType.GravityWell)
                {
                    continue;
                }

                if (!TryGetTargetOrbit(orbit, gravityWellData.Type, out Orbit targetOrbit))
                {
                    continue;
                }

                _gravityWellFactory.Create(orbitObject.transform, orbit, gravityWellData, targetOrbit);
                GravityWell gravityWell = (GravityWell)gravityWellData.Object;
                _zoneRegistry.Register(new GravityWellZone(
                    _nextZoneId++,
                    orbit,
                    targetOrbit,
                    gravityWellData.Type,
                    gravityWellData.Arc,
                    gravityWell.PartialJumpStep,
                    gravityWell.PullSpeed,
                    gravityWell.ReturnDuration));
            }
        }

        public void Clear(bool retainVisibility = false)
        {
            _gravityWellFactory.Clear();
            _zoneRegistry.Clear();
            _nextZoneId = 0;
        }

        private bool TryGetTargetOrbit(Orbit sourceOrbit, ArcType sourceSide, out Orbit targetOrbit)
        {
            int targetIndex = sourceSide switch
            {
                ArcType.Inner => sourceOrbit.Index - 1,
                ArcType.Outer => sourceOrbit.Index + 1,
                _ => -1
            };

            if (targetIndex < 0 || targetIndex >= _levelState.Count)
            {
                targetOrbit = null;
                return false;
            }

            targetOrbit = _levelState.GetOrbit(targetIndex);
            return true;
        }
    }
}
