using System.Collections.Generic;
using UnityEngine;

namespace OrbitalEscape.Rifts
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class RiftController : MonoBehaviour
    {
        private static readonly int RiftCountId = Shader.PropertyToID("_RiftCount");
        private static readonly int RiftsId = Shader.PropertyToID("_Rifts");
        
        [SerializeField] private List<Rift> _rifts;
        
        private readonly Vector4[] _riftsPacked = new Vector4[32];
        private MaterialPropertyBlock _materialPropertyBlock;
        private Renderer _cachedRenderer;
        
        public IReadOnlyList<Rift> Rifts => _rifts;

        private void Awake()
        {
            _cachedRenderer = GetComponent<Renderer>();
            _materialPropertyBlock = new MaterialPropertyBlock();
            this.Apply();
        }

        private void OnEnable()
        {
            this.Apply();
        }

        private void OnValidate()
        {
            if (_cachedRenderer == null)
            {
                _cachedRenderer = GetComponent<Renderer>();
            }

            _materialPropertyBlock ??= new MaterialPropertyBlock();

            Apply();
        }

        private void Apply()
        {
            int count = 0;

            for (int i = 0; i < _rifts.Count && count < 32; i++)
            {
                var rift = _rifts[i];
                if (rift == null || !rift.Enabled || rift.ArcLength <= 0.0f)
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
    }
}
