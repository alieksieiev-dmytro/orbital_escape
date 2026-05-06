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

        public void Spawn(GameObject orbitObject, Orbit orbit)
        {
            foreach (ArcObject impulseTrap in orbit.GetOrbitObjectsByType(ArcObjectType.ImpulseTrap))
            {
                _impulseTrapFactory.Create(orbitObject.transform, orbit, impulseTrap);
            }
        }

        public void Clear()
        {
            _impulseTrapFactory.Clear();
        }
    }
}
