using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class PaletteMaterialColorTarget : MonoBehaviour, IPaletteTarget
    {
        [SerializeField] private Renderer _targetRenderer;
        [SerializeField] private string _colorProperty = "_Color";
        [SerializeField] private PaletteColorType _colorType = PaletteColorType.Accent;
        [SerializeField] private bool _preserveCurrentAlpha = true;

        private MaterialPropertyBlock _propertyBlock;
        private int _colorPropertyId;

        private void Reset()
        {
            ResolveRenderer();
        }

        private void Awake()
        {
            ResolveRenderer();
            _propertyBlock = new MaterialPropertyBlock();
            _colorPropertyId = Shader.PropertyToID(_colorProperty);
        }

        public void ApplyPalette(PaletteSnapshot palette)
        {
            if (_targetRenderer == null || string.IsNullOrEmpty(_colorProperty))
            {
                return;
            }

            Material material = _targetRenderer.sharedMaterial;
            if (material == null)
            {
                return;
            }

            if (_colorPropertyId == 0)
            {
                _colorPropertyId = Shader.PropertyToID(_colorProperty);
            }

            if (!material.HasProperty(_colorPropertyId))
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.Clear();
            _targetRenderer.GetPropertyBlock(_propertyBlock);

            Color color = palette.GetColor(_colorType);
            if (_preserveCurrentAlpha)
            {
                color.a = material.GetColor(_colorPropertyId).a;
            }

            _propertyBlock.SetColor(_colorPropertyId, color);
            _targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void ResolveRenderer()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<Renderer>();
            }
        }
    }
}
