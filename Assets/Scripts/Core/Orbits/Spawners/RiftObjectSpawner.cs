using System.Collections.Generic;
using OrbitalEscape.Factories;
using OrbitalEscape.Visuals;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class RiftObjectSpawner : IOrbitObjectSpawner
    {
        private static readonly IReadOnlyList<ArcObject> NoActiveRifts = System.Array.Empty<ArcObject>();

        private readonly IObjectFactory<RiftObject> _riftFactory;
        private readonly RiftConfig _riftConfig;
        private readonly IRiftClosureState _riftClosureState;

        public int Order => 0;

        [Inject]
        public RiftObjectSpawner(
            [Inject(Id = FactoryType.RiftFactory)] IObjectFactory<RiftObject> riftFactory,
            RiftConfig riftConfig,
            IRiftClosureState riftClosureState)
        {
            _riftFactory = riftFactory;
            _riftConfig = riftConfig;
            _riftClosureState = riftClosureState;
        }

        public void Spawn(GameObject orbitObject, Orbit orbit, IReadOnlyList<ArcObject> objects)
        {
            bool riftsClosed = _riftClosureState.IsClosed(orbit.Index);
            IReadOnlyList<ArcObject> activeRifts = riftsClosed
                ? NoActiveRifts
                : orbit.GetOrbitObjectsByType(ArcObjectType.Rift);

            orbitObject.GetComponent<RiftController>().Initialize(activeRifts);
            orbitObject.GetComponent<AuroraRiftClearanceController>()?.Apply(
                activeRifts,
                _riftConfig.AuroraRiftAlphaMultiplier);

            if (riftsClosed)
            {
                DisableRiftColliders(orbitObject);
                return;
            }

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

        private void DisableRiftColliders(GameObject orbitObject)
        {
            foreach (RiftObject riftObject in orbitObject.GetComponentsInChildren<RiftObject>(true))
            {
                riftObject.SetColliderActive(false);
            }
        }
    }
}
