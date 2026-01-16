using System.Collections.Generic;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class RiftController : MonoBehaviour
    {
        private static readonly int RiftCountId = Shader.PropertyToID("_RiftCount");
        private static readonly int RiftsId = Shader.PropertyToID("_Rifts");
        
        private readonly Vector4[] _riftsPacked = new Vector4[32];
        private MaterialPropertyBlock _materialPropertyBlock;
        private Renderer _cachedRenderer;
        private List<Rift> _rifts;

        public void Initialize(List<Rift> rifts)
        {
            _rifts = rifts;
            Apply();
        }

        private void Apply()
        {
            if (_rifts == null) return;
            
            int count = 0;

            for (int i = 0; i < _rifts.Count && count < 32; i++)
            {
                var rift = _rifts[i];
                if (Equals(rift, default(Rift)) || rift.ArcLength <= 0.0f)
                {
                    continue;
                }

                float radians = Mathf.Deg2Rad * Mathf.Repeat(rift.StartAngleDegrees, 360.0f);
                _riftsPacked[count] = new Vector4(radians, rift.ArcLength, 0.0f, 1.0f);
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
