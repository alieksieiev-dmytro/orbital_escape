using System.Collections.Generic;
using OrbitalEscape.Utils;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class RiftController : MonoBehaviour
    {
        [SerializeField] private GameObject _riftColliderPrefab;
        
        private static readonly int RiftCountId = Shader.PropertyToID("_RiftCount");
        private static readonly int RiftsId = Shader.PropertyToID("_Rifts");
        
        private readonly Vector4[] _riftsPacked = new Vector4[32];
        private MaterialPropertyBlock _materialPropertyBlock;
        private Renderer _cachedRenderer;
        private List<ArcObject> _rifts;

        public void Initialize(List<ArcObject> rifts)
        {
            _rifts = rifts;
            Apply();
            CreateColliders();
        }

        private void Apply()
        {
            if (_rifts == null) return;
            
            int count = 0;

            for (int i = 0; i < _rifts.Count && count < 32; i++)
            {
                var rift = _rifts[i];
                if (Equals(rift.Arc, default(Arc)) || rift.Arc.Length() <= 0.0f)
                {
                    continue;
                }

                float radians = Mathf.Deg2Rad * Mathf.Repeat(rift.Arc.StartAngleDegree, 360.0f);
                _riftsPacked[count] = new Vector4(radians, rift.Arc.Length(), 0.0f, 1.0f);
                count++;
            }

            for (int i = count; i < 32; i++)
            {
                _riftsPacked[i] = Vector4.zero;
            }

            _cachedRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetInt(RiftCountId, count);
            _materialPropertyBlock.SetVectorArray(RiftsId, _riftsPacked);
            _cachedRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        private void CreateColliders()
        {
            foreach (var arcObject in _rifts)
            {
                var position = OrbitMath.GetPointOnCircle(Vector2.zero, arcObject.Arc.Radius,
                    arcObject.Arc.MidAngleDegree());
                var rotation = Quaternion.Euler(0, 0, OrbitMath.GetZRotation2D(Vector2.zero, position));
                
                var colliderObj = Instantiate(
                    _riftColliderPrefab,
                    position,
                    rotation,
                    transform);

                var scale = new Vector3(1, arcObject.Arc.Length() * 0.9f, 1);
                scale.x /= transform.lossyScale.x;
                scale.y /= transform.lossyScale.y;
                colliderObj.transform.localScale = scale;
            }
        }

        private void Awake()
        {
            _cachedRenderer = GetComponent<Renderer>();
            _materialPropertyBlock = new MaterialPropertyBlock();
            Apply();
        }

        private void OnEnable() => Apply();

        private void OnValidate()
        {
            if (_cachedRenderer == null) _cachedRenderer = GetComponent<Renderer>();

            _materialPropertyBlock ??= new MaterialPropertyBlock();

            Apply();
        }
    }
}
