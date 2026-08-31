using System;
using OrbitalEscape.Orbits;
using OrbitalEscape.Pooling;
using OrbitalEscape.Utils;
using UnityEngine;

namespace OrbitalEscape.Factories
{
    public sealed class SpikeFactory : ISpikeFactory, IDisposable
    {
        private readonly GameObject _spikePrefab;
        private readonly Transform _poolRoot;
        private readonly IObjectPool<GameObject> _pool;

        public int CountAll => _pool.CountAll;
        public int CountActive => _pool.CountActive;
        public int CountInactive => _pool.CountInactive;

        public SpikeFactory(GameObject spikePrefab, ObjectPoolConfig poolConfig)
        {
            _spikePrefab = spikePrefab != null
                ? spikePrefab
                : throw new ArgumentNullException(nameof(spikePrefab));

            _poolRoot = new GameObject("[Pool] Spikes").transform;
            _poolRoot.gameObject.SetActive(false);

            _pool = new ObjectPool<GameObject>(
                CreatePooledSpike,
                OnRent,
                OnReturn,
                UnityEngine.Object.Destroy,
                poolConfig != null ? poolConfig.SpikePrewarmCount : 0,
                IsValidSpike);
        }

        public GameObject Create(Transform root, Orbit orbitData, ArcObject spikeData)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            GameObject spikeObj = _pool.Rent();
            spikeObj.name = $"spike_{spikeData.Type}";
            spikeObj.transform.SetParent(root, false);

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

            Collider2D[] colliders = spikeObj.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = true;
            }

            return spikeObj;
        }

        public bool Release(GameObject spike)
        {
            return _pool.Return(spike);
        }

        public void ReleaseAll()
        {
            _pool.ReturnAll();
        }

        public void Dispose()
        {
            _pool.Dispose();
            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
            }
        }

        private GameObject CreatePooledSpike()
        {
            GameObject spike = UnityEngine.Object.Instantiate(
                _spikePrefab,
                Constants.Center3D,
                Quaternion.identity,
                _poolRoot);
            spike.SetActive(false);
            return spike;
        }

        private void OnRent(GameObject spike)
        {
            spike.SetActive(true);
        }

        private void OnReturn(GameObject spike)
        {
            spike.SetActive(false);
            spike.transform.SetParent(_poolRoot, false);
            spike.transform.localPosition = Vector3.zero;
            spike.transform.localRotation = Quaternion.identity;
            spike.transform.localScale = Vector3.one;
        }

        private static bool IsValidSpike(GameObject spike)
        {
            return spike != null;
        }
    }
}
