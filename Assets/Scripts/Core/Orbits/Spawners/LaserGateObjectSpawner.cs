using System.Collections.Generic;
using OrbitalEscape.Factories;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class LaserGateObjectSpawner : IOrbitObjectSpawner
    {
        private readonly IObjectFactory<GameObject> _laserGateFactory;
        private readonly LevelState _levelState;

        public int Order => 35;

        [Inject]
        public LaserGateObjectSpawner(
            [Inject(Id = FactoryType.LaserGateFactory)] IObjectFactory<GameObject> laserGateFactory,
            LevelState levelState)
        {
            _laserGateFactory = laserGateFactory;
            _levelState = levelState;
        }

        public void Spawn(GameObject orbitObject, Orbit orbit, IReadOnlyList<ArcObject> objects)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                ArcObject laserGate = objects[i];
                if (laserGate.Object.Type != ArcObjectType.LaserGate)
                {
                    continue;
                }

                if (!TryGetTargetOrbit(orbit, laserGate.Type, out Orbit targetOrbit))
                {
                    continue;
                }

                _laserGateFactory.Create(orbitObject.transform, orbit, laserGate, targetOrbit);
            }
        }

        public void Clear()
        {
            _laserGateFactory.Clear();
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
