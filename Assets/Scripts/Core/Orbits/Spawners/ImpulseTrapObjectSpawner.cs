using System.Collections.Generic;
using OrbitalEscape.Factories;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class ImpulseTrapObjectSpawner : IOrbitObjectSpawner
    {
        private readonly IObjectFactory<GameObject> _impulseTrapFactory;

        public int Order => 20;

        [Inject]
        public ImpulseTrapObjectSpawner([Inject(Id = FactoryType.ImpulseTrapFactory)] IObjectFactory<GameObject> impulseTrapFactory)
        {
            _impulseTrapFactory = impulseTrapFactory;
        }

        public void Spawn(GameObject orbitObject, Orbit orbit, IReadOnlyList<ArcObject> objects)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                ArcObject impulseTrap = objects[i];
                if (impulseTrap.Object.Type != ArcObjectType.ImpulseTrap)
                {
                    continue;
                }

                _impulseTrapFactory.Create(orbitObject.transform, orbit, impulseTrap);
            }
        }

        public void Clear(bool retainVisibility = false)
        {
            _impulseTrapFactory.Clear();
        }
    }
}
