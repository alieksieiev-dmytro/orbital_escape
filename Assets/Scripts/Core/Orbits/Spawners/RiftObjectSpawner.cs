using System.Collections.Generic;
using OrbitalEscape.Factories;
using OrbitalEscape.Visuals;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class RiftObjectSpawner : IOrbitObjectSpawner
    {
        private readonly IObjectFactory<RiftObject> _riftFactory;
        private readonly RiftConfig _riftConfig;

        public int Order => 0;

        [Inject]
        public RiftObjectSpawner(
            [Inject(Id = FactoryType.RiftFactory)] IObjectFactory<RiftObject> riftFactory,
            RiftConfig riftConfig)
        {
            _riftFactory = riftFactory;
            _riftConfig = riftConfig;
        }

        public void Spawn(GameObject orbitObject, Orbit orbit, IReadOnlyList<ArcObject> objects)
        {
            var rifts = orbit.GetOrbitObjectsByType(ArcObjectType.Rift);
            orbitObject.GetComponent<RiftController>().Initialize(rifts);
            orbitObject.GetComponent<AuroraRiftClearanceController>()?.Apply(
                rifts,
                _riftConfig.AuroraRiftAlphaMultiplier);

            for (int i = 0; i < objects.Count; i++)
            {
                ArcObject rift = objects[i];
                if (rift.Object.Type != ArcObjectType.Rift)
                {
                    continue;
                }

                _riftFactory.Create(orbitObject.transform, rift);
            }
        }

        public void Clear()
        {
            _riftFactory.Clear();
        }
    }
}
