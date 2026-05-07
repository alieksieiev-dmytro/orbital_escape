using System.Collections.Generic;
using OrbitalEscape.Utils;
using OrbitalEscape.Visuals.Palettes;
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
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        
        private readonly Vector4[] _riftsPacked = new Vector4[32];
        private MaterialPropertyBlock _materialPropertyBlock;
        private Renderer _cachedRenderer;
        private List<ArcObject> _rifts;
        private Color _paletteColor;
        private bool _hasPaletteColor;

        public void Initialize(List<ArcObject> rifts)
        {
            _rifts = rifts;
            Apply();
        }

        public void ApplyPalette(PaletteSnapshot palette)
        {
            _paletteColor = palette.OutlineColor;
            _hasPaletteColor = true;
            Apply();
        }

        private void Apply()
        {
            if (_materialPropertyBlock == null || _cachedRenderer == null)
            {
                return;
            }

            if (_rifts == null && !_hasPaletteColor)
            {
                return;
            }
            
            int count = 0;

            for (int i = 0; _rifts != null && i < _rifts.Count && count < 32; i++)
            {
                var rift = _rifts[i];
                if (Equals(rift.Arc, default(Arc)) || rift.Arc.Length() <= 0.0f)
                {
                    continue;
                }

                float padding = ((Rift)rift.Object).Padding;
                float angleShift = (padding / 2) / rift.Arc.Radius * Mathf.Rad2Deg;
                float radians = Mathf.Deg2Rad * Mathf.Repeat(rift.Arc.StartAngleDegree + angleShift, 360.0f);
                _riftsPacked[count] = new Vector4(radians, rift.Arc.Length() - padding, 0.0f, 1.0f);
                count++;
            }

            for (int i = count; i < 32; i++)
            {
                _riftsPacked[i] = Vector4.zero;
            }

            _cachedRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetInt(RiftCountId, count);
            _materialPropertyBlock.SetVectorArray(RiftsId, _riftsPacked);
            if (_hasPaletteColor)
            {
                _materialPropertyBlock.SetColor(ColorId, _paletteColor);
            }
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
