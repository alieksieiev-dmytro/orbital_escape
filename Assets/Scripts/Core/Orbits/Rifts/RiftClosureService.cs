using System;
using System.Collections.Generic;
using OrbitalEscape.Events;
using OrbitalEscape.Visuals;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class RiftClosureService : IInitializable, IDisposable
    {
        private static readonly IReadOnlyList<ArcObject> NoActiveRifts = Array.Empty<ArcObject>();

        private readonly IRiftClosureState _closureState;
        private readonly ILevelObjectSpawner _levelObjectSpawner;
        private readonly RiftConfig _riftConfig;

        private EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitBinding;

        [Inject]
        public RiftClosureService(
            IRiftClosureState closureState,
            ILevelObjectSpawner levelObjectSpawner,
            RiftConfig riftConfig)
        {
            _closureState = closureState;
            _levelObjectSpawner = levelObjectSpawner;
            _riftConfig = riftConfig;
        }

        public void Initialize()
        {
            _passedThroughOrbitBinding = new EventBinding<PassedThroughOrbitEvent>(OnPassedThroughOrbit);
            EventBus<PassedThroughOrbitEvent>.Register(_passedThroughOrbitBinding);
        }

        public void Dispose()
        {
            if (_passedThroughOrbitBinding != null)
            {
                EventBus<PassedThroughOrbitEvent>.Deregister(_passedThroughOrbitBinding);
            }
        }

        private void OnPassedThroughOrbit(PassedThroughOrbitEvent @event)
        {
            Orbit orbit = @event.Orbit;
            if (orbit == null || !_closureState.Close(orbit.Index))
            {
                return;
            }

            RefreshClosedOrbit(orbit);
        }

        private void RefreshClosedOrbit(Orbit orbit)
        {
            if (orbit.Index < 0 || orbit.Index >= _levelObjectSpawner.SpawnedOrbitObjects.Count)
            {
                return;
            }

            GameObject orbitObject = _levelObjectSpawner.GetOrbitObject(orbit.Index);
            if (orbitObject == null)
            {
                return;
            }

            foreach (RiftObject riftObject in orbitObject.GetComponentsInChildren<RiftObject>(true))
            {
                riftObject.SetColliderActive(false);
            }

            orbitObject.GetComponent<RiftController>()?.Initialize(NoActiveRifts);
            orbitObject.GetComponent<AuroraRiftClearanceController>()?.Apply(
                NoActiveRifts,
                _riftConfig.AuroraRiftAlphaMultiplier);
        }
    }
}
