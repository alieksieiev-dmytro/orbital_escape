using System;
using System.Collections.Generic;
using OrbitalEscape.Events;
using OrbitalEscape.Factories;
using OrbitalEscape.Movement;
using OrbitalEscape.Visuals.Palettes;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class SpikeObjectSpawner : IOrbitObjectSpawner, IInitializable, ITickable, IDisposable
    {
        private readonly ISpikeFactory _spikeFactory;
        private readonly IOrbitJumpTransitionService _jumpTransitionService;
        private readonly IPaletteService _paletteService;
        private readonly IPaletteObjectPainter _paletteObjectPainter;
        private readonly Dictionary<int, GameObject> _orbitObjects = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, Orbit> _orbits = new Dictionary<int, Orbit>();
        private readonly Dictionary<int, List<ArcObject>> _spikeData = new Dictionary<int, List<ArcObject>>();
        private readonly Dictionary<int, List<GameObject>> _activeSpikes = new Dictionary<int, List<GameObject>>();
        private readonly HashSet<int> _visibleOrbitIndices = new HashSet<int>();
        private readonly HashSet<int> _activeOrbitIndices = new HashSet<int>();
        private readonly HashSet<int> _pendingReleaseIndices = new HashSet<int>();

        private EventBinding<OrbitsSetEvent> _orbitsSetBinding;

        public int Order => 10;

        [Inject]
        public SpikeObjectSpawner(
            ISpikeFactory spikeFactory,
            IOrbitJumpTransitionService jumpTransitionService,
            IPaletteService paletteService,
            IPaletteObjectPainter paletteObjectPainter)
        {
            _spikeFactory = spikeFactory;
            _jumpTransitionService = jumpTransitionService;
            _paletteService = paletteService;
            _paletteObjectPainter = paletteObjectPainter;
        }

        public void Initialize()
        {
            _orbitsSetBinding = new EventBinding<OrbitsSetEvent>(OnOrbitsSet);
            EventBus<OrbitsSetEvent>.Register(_orbitsSetBinding);
        }

        public void Tick()
        {
            if (_pendingReleaseIndices.Count == 0 || _jumpTransitionService.IsTransitioning)
            {
                return;
            }

            var pendingIndices = new List<int>(_pendingReleaseIndices);
            _pendingReleaseIndices.Clear();

            for (int i = 0; i < pendingIndices.Count; i++)
            {
                int orbitIndex = pendingIndices[i];
                if (!_visibleOrbitIndices.Contains(orbitIndex))
                {
                    ReleaseOrbit(orbitIndex);
                }
            }
        }

        public void Spawn(GameObject orbitObject, Orbit orbit, IReadOnlyList<ArcObject> objects)
        {
            if (orbitObject == null || orbit == null || objects == null)
            {
                return;
            }

            int orbitIndex = orbit.Index;
            _orbitObjects[orbitIndex] = orbitObject;
            _orbits[orbitIndex] = orbit;

            if (!_spikeData.TryGetValue(orbitIndex, out List<ArcObject> registeredSpikes))
            {
                registeredSpikes = new List<ArcObject>();
                _spikeData.Add(orbitIndex, registeredSpikes);
            }

            var addedSpikes = new List<ArcObject>();
            for (int i = 0; i < objects.Count; i++)
            {
                ArcObject candidate = objects[i];
                if (candidate.Object.Type != ArcObjectType.Spike)
                {
                    continue;
                }

                registeredSpikes.Add(candidate);
                addedSpikes.Add(candidate);
            }

            if (!_visibleOrbitIndices.Contains(orbitIndex))
            {
                return;
            }

            if (_activeOrbitIndices.Contains(orbitIndex))
            {
                SpawnSpikes(orbitIndex, addedSpikes);
            }
            else
            {
                ActivateOrbit(orbitIndex);
            }
        }

        public void Clear(bool retainVisibility = false)
        {
            var activeIndices = new List<int>(_activeOrbitIndices);
            for (int i = 0; i < activeIndices.Count; i++)
            {
                ReleaseOrbit(activeIndices[i]);
            }

            _orbitObjects.Clear();
            _orbits.Clear();
            _spikeData.Clear();
            if (!retainVisibility)
                _visibleOrbitIndices.Clear();
            _activeSpikes.Clear();
            _activeOrbitIndices.Clear();
            _pendingReleaseIndices.Clear();
        }

        public void Dispose()
        {
            if (_orbitsSetBinding != null)
            {
                EventBus<OrbitsSetEvent>.Deregister(_orbitsSetBinding);
            }

            Clear();
        }

        private void OnOrbitsSet(OrbitsSetEvent @event)
        {
            var nextVisibleIndices = new HashSet<int>();
            if (@event.CurrentOrbit != null)
            {
                nextVisibleIndices.Add(@event.CurrentOrbit.Index);
            }

            if (@event.OppositeOrbit != null)
            {
                nextVisibleIndices.Add(@event.OppositeOrbit.Index);
            }

            var leavingIndices = new List<int>();
            foreach (int activeIndex in _activeOrbitIndices)
            {
                if (!nextVisibleIndices.Contains(activeIndex))
                {
                    leavingIndices.Add(activeIndex);
                }
            }

            _visibleOrbitIndices.Clear();
            foreach (int visibleIndex in nextVisibleIndices)
            {
                _visibleOrbitIndices.Add(visibleIndex);
                _pendingReleaseIndices.Remove(visibleIndex);
            }

            if (!_jumpTransitionService.IsTransitioning)
            {
                for (int i = 0; i < leavingIndices.Count; i++)
                {
                    ReleaseOrbit(leavingIndices[i]);
                }
            }

            foreach (int visibleIndex in _visibleOrbitIndices)
            {
                ActivateOrbit(visibleIndex);
            }

            if (_jumpTransitionService.IsTransitioning)
            {
                for (int i = 0; i < leavingIndices.Count; i++)
                {
                    _pendingReleaseIndices.Add(leavingIndices[i]);
                }
            }
        }

        private void ActivateOrbit(int orbitIndex)
        {
            if (_activeOrbitIndices.Contains(orbitIndex) ||
                !_orbitObjects.ContainsKey(orbitIndex) ||
                !_orbits.ContainsKey(orbitIndex))
            {
                return;
            }

            _activeOrbitIndices.Add(orbitIndex);
            if (!_activeSpikes.ContainsKey(orbitIndex))
            {
                _activeSpikes.Add(orbitIndex, new List<GameObject>());
            }

            if (_spikeData.TryGetValue(orbitIndex, out List<ArcObject> spikes))
            {
                SpawnSpikes(orbitIndex, spikes);
            }
        }

        private void SpawnSpikes(int orbitIndex, IReadOnlyList<ArcObject> spikes)
        {
            if (spikes == null || spikes.Count == 0 ||
                !_orbitObjects.TryGetValue(orbitIndex, out GameObject orbitObject) ||
                !_orbits.TryGetValue(orbitIndex, out Orbit orbit))
            {
                return;
            }

            if (!_activeSpikes.TryGetValue(orbitIndex, out List<GameObject> instances))
            {
                instances = new List<GameObject>();
                _activeSpikes.Add(orbitIndex, instances);
            }

            for (int i = 0; i < spikes.Count; i++)
            {
                GameObject spike = _spikeFactory.Create(orbitObject.transform, orbit, spikes[i]);
                _paletteObjectPainter.ApplyLevelObject(spike, _paletteService.CurrentPalette);
                instances.Add(spike);
            }
        }

        private void ReleaseOrbit(int orbitIndex)
        {
            _pendingReleaseIndices.Remove(orbitIndex);
            _activeOrbitIndices.Remove(orbitIndex);

            if (!_activeSpikes.TryGetValue(orbitIndex, out List<GameObject> spikes))
            {
                return;
            }

            for (int i = 0; i < spikes.Count; i++)
            {
                _spikeFactory.Release(spikes[i]);
            }

            _activeSpikes.Remove(orbitIndex);
        }
    }
}
