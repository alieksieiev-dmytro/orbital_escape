using System.Collections.Generic;
using OrbitalEscape.Orbits;
using OrbitalEscape.Utils;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Factories
{
    public sealed class RiftFactory : IObjectFactory<RiftObject>
    {
        private readonly RiftConfig _config;
        private readonly RiftObject _riftPrefab;
        private readonly List<RiftObject> _riftObjects;
        
        [Inject]
        public RiftFactory(RiftObject riftPrefab, RiftConfig config)
        {
            _config = config;
            _riftPrefab = riftPrefab;
            _riftObjects = new List<RiftObject>();
        }
        
        public RiftObject Create(params object[] args)
        {
            var root = (Transform)args[0];
            var riftData = (ArcObject)args[1];
            
            var riftObj = CreateCollider(riftData, root);
            _riftObjects.Add(riftObj);
            
            return riftObj;
        }
        
        public RiftObject Get(int index) => _riftObjects[index];
        
        public void Clear()
        {
            for (int i = _riftObjects.Count - 1; i >= 0; i--)
            {
                Object.Destroy(_riftObjects[i].gameObject);
            }
            _riftObjects.Clear();
        }

        private RiftObject CreateCollider(ArcObject rift, Transform parent)
        {
            var position = OrbitMath.GetPointOnCircle(Vector2.zero, rift.Arc.Radius,
                rift.Arc.MidAngleDegree());
            var rotation = Quaternion.Euler(0, 0, OrbitMath.GetZRotation2D(Vector2.zero, position));

            var riftObject = Object.Instantiate(
                _riftPrefab,
                position,
                rotation,
                parent);

            float padding = rift.Arc.Length() - _config.RiftPaddings;
            var arcScale = new Vector3(1, rift.Arc.Length(), 1);
            var colliderScale = new Vector3(1f, padding / rift.Arc.Length(), 1f);
            arcScale.x /= parent.lossyScale.x;
            arcScale.y /= parent.lossyScale.y;
            riftObject.SetScale(arcScale, colliderScale);
            riftObject.transform.localScale = arcScale;
            
            return riftObject;
        }
    }
}
