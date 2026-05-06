using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Orbits;
using OrbitalEscape.Utils;
using UnityEngine;

namespace OrbitalEscape.Factories
{
    public sealed class SpikeFactory : IObjectFactory<GameObject>
    {
        private readonly List<GameObject> _spikeObjects;
        private readonly GameObject _spikePrefab;

        public SpikeFactory(GameObject spikePrefab)
        {
            _spikeObjects = new List<GameObject>();
            _spikePrefab = spikePrefab;
        }

        public GameObject Create(params object[] args)
        {
            var root = (Transform)args[0];
            var orbitData = (Orbit)args[1];
            var spikeData = (ArcObject)args[2];

            var spikeObj = Object.Instantiate(_spikePrefab, Constants.Center3D, Quaternion.identity, root);
            spikeObj.name = $"spike_{spikeData.Type}";

            spikeObj.transform.localScale =
                new Vector3(spikeData.Arc.Length() / root.localScale.x,
                    ((SpikeObstacle)spikeData.Object).Height / root.localScale.y,
                    1f);

            var angleDelta = OrbitMath.DeltaDegrees(spikeData.Arc.StartAngleDegree, spikeData.Arc.EndAngleDegree) / 2f;
            var angle = spikeData.Arc.StartAngleDegree + angleDelta * 0.5f;

            float radius = spikeData.Type switch
            {
                ArcType.Inner => orbitData.Radius - 0.1f,
                ArcType.Outer => orbitData.Radius,
                _ => 0f
            };

            var position = OrbitMath.GetPointOnCircle(Constants.Center, radius, angle) / root.localScale.x;

            Vector2 dir = spikeData.Type switch
            {
                ArcType.Inner => Constants.Center - position,
                ArcType.Outer => position - Constants.Center,
                _ => Vector2.zero
            };

            var rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            spikeObj.transform.rotation = Quaternion.Euler(0f, 0f, rotation + 90.0f);
            spikeObj.transform.localPosition = new Vector3(position.x, position.y, 0f);
            _spikeObjects.Add(spikeObj);

            return spikeObj;
        }

        public GameObject Get(int index) => _spikeObjects[index];
        
        public void Clear()
        {
            foreach (var spike in _spikeObjects)
            {
                Object.Destroy(spike);
            }
            _spikeObjects.Clear();
        }
    }
}
