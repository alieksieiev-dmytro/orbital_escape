using System;
using System.Collections.Generic;
using System.Linq;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class LevelBuildContext
    {
        private readonly List<Orbit> _orbits;
        private readonly List<string> _warnings;
        private readonly Dictionary<int, List<ArcObject>> _stagedExistingOrbitObjects;

        public IReadOnlyList<Orbit> Orbits => _orbits;
        public IReadOnlyList<string> Warnings => _warnings;
        public int StartIndex { get; }
        public int LastPlannedOrbitIndex { get; }
        public LevelBuildSettings Settings { get; }
        public LevelRandom Random { get; }

        public LevelBuildContext(
            IEnumerable<Orbit> existingOrbits,
            int buildCount,
            LevelBuildSettings settings,
            LevelRandom random)
        {
            _orbits = existingOrbits.ToList();
            _warnings = new List<string>();
            _stagedExistingOrbitObjects = new Dictionary<int, List<ArcObject>>();
            StartIndex = _orbits.Count;
            LastPlannedOrbitIndex = StartIndex + buildCount - 1;
            Settings = settings;
            Random = random;
        }

        public bool TryGetPreviousOrbit(out Orbit orbit)
        {
            if (_orbits.Count == 0)
            {
                orbit = null;
                return false;
            }

            orbit = _orbits[^1];
            return true;
        }

        internal void RegisterBuiltOrbit(Orbit orbit)
        {
            _orbits.Add(orbit);
        }

        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }

        public void StageExistingOrbitObject(Orbit orbit, ArcObject arcObject)
        {
            if (orbit == null || orbit.Index >= StartIndex)
            {
                return;
            }

            if (!_stagedExistingOrbitObjects.TryGetValue(orbit.Index, out List<ArcObject> objects))
            {
                objects = new List<ArcObject>();
                _stagedExistingOrbitObjects.Add(orbit.Index, objects);
            }

            objects.Add(arcObject);
        }

        public IReadOnlyList<ArcObject> GetStagedExistingOrbitObjects(Orbit orbit)
        {
            if (orbit != null &&
                _stagedExistingOrbitObjects.TryGetValue(orbit.Index, out List<ArcObject> objects))
            {
                return objects;
            }

            return Array.Empty<ArcObject>();
        }

        public IReadOnlyList<OrbitContentDelta> CreateContentDeltas()
        {
            List<OrbitContentDelta> deltas = new List<OrbitContentDelta>(_stagedExistingOrbitObjects.Count);
            foreach (KeyValuePair<int, List<ArcObject>> pair in _stagedExistingOrbitObjects.OrderBy(pair => pair.Key))
            {
                deltas.Add(new OrbitContentDelta(pair.Key, new List<ArcObject>(pair.Value)));
            }

            return deltas;
        }
    }
}
