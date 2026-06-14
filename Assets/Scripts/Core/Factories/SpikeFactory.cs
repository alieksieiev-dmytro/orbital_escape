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
                new Vector3(spikeData.Arc.Length() / root.lossyScale.x,
                    ((SpikeObstacle)spikeData.Object).Height / root.lossyScale.y,
                    1f);

            var angleDelta = OrbitMath.DeltaDegrees(spikeData.Arc.StartAngleDegree, spikeData.Arc.EndAngleDegree) / 2f;
            var angle = spikeData.Arc.StartAngleDegree + angleDelta * 0.5f;

            float radius = spikeData.Type switch
            {
                ArcType.Outer => orbitData.Radius - 0.1f,
                ArcType.Inner => orbitData.Radius,
                _ => 0f
            };

            Vector2 worldPosition = OrbitMath.GetPointOnCircle(Constants.Center, radius, angle);

            Vector2 dir = spikeData.Type switch
            {
                ArcType.Outer => Constants.Center - worldPosition,
                ArcType.Inner => worldPosition - Constants.Center,
                _ => Vector2.zero
            };

            var rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Vector2 offsetWorldPosition = OrbitMath.AddWorldOffset(worldPosition, dir, Constants.OBSTACLE_Y_OFFSET);
            Vector2 localPosition = OrbitMath.WorldToLocalPoint(root, offsetWorldPosition);

            spikeObj.transform.rotation = Quaternion.Euler(0f, 0f, rotation + 90.0f);
            spikeObj.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
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
