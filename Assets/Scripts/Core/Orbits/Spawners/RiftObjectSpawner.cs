using OrbitalEscape.Factories;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class RiftObjectSpawner : IOrbitObjectSpawner
    {
        private readonly IObjectFactory<RiftObject> _riftFactory;

        public int Order => 0;

        [Inject]
        public RiftObjectSpawner([Inject(Id = FactoryType.RiftFactory)] IObjectFactory<RiftObject> riftFactory)
        {
            _riftFactory = riftFactory;
        }

        public void Spawn(GameObject orbitObject, Orbit orbit)
        {
            var rifts = orbit.GetOrbitObjectsByType(ArcObjectType.Rift);
            orbitObject.GetComponent<RiftController>().Initialize(rifts);

            foreach (ArcObject rift in rifts)
            {
                _riftFactory.Create(orbitObject.transform, rift);
            }
        }

        public void Clear()
        {
            _riftFactory.Clear();
        }
    }
}
