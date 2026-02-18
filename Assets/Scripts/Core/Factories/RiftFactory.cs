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
            var orbitData = (Orbit)args[1];
            var riftData = orbitData.GetOrbitObjectsByType(ArcObjectType.Rift);
            
            CreateColliders(riftData, root);
            root.GetComponent<RiftController>().Initialize(riftData);

            var riftObj = Object.Instantiate(_riftPrefab, Constants.Center3D, Quaternion.identity, root);
            
            return riftObj;
        }
        
        public RiftObject Get(int index)
        {
            throw new System.NotImplementedException();
        }
        
        public void Clear()
        {
        }

        private void CreateColliders(List<ArcObject> rifts, Transform parent)
        {
            foreach (var arcObject in rifts)
            {
                var position = OrbitMath.GetPointOnCircle(Vector2.zero, arcObject.Arc.Radius,
                    arcObject.Arc.MidAngleDegree());
                var rotation = Quaternion.Euler(0, 0, OrbitMath.GetZRotation2D(Vector2.zero, position));
                
                var riftObject = Object.Instantiate(
                    _riftPrefab,
                    position,
                    rotation,
                    parent);

                var arcScale = new Vector3(1, arcObject.Arc.Length(), 1);
                var colliderScale = new Vector3(arcScale.x - _config.RiftPaddings, arcScale.y, 1f);
                arcScale.x /= parent.lossyScale.x;
                arcScale.y /= parent.lossyScale.y;
                riftObject.SetScale(arcScale, colliderScale);
                riftObject.transform.localScale = arcScale;
                
                _riftObjects.Add(riftObject);
            }
        }
    }
}
