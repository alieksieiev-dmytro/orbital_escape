using TMPro;
using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [DisallowMultipleComponent]
    public sealed class PaletteTextGlowTarget : MonoBehaviour, IPaletteTarget
    {
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");

        private TMP_Text _text;
        private Material _originalMaterial;
        private Material _runtimeMaterial;

        private void Awake()
        {
            EnsureRuntimeMaterial();
        }

        private void OnDestroy()
        {
            RestoreOriginalMaterial();
            DestroyRuntimeMaterial();
        }

        public void ApplyPalette(PaletteSnapshot palette)
        {
            if (!EnsureRuntimeMaterial())
            {
                return;
            }

            Color glowColor = palette.OutlineColor;
            glowColor.a = Constants.FONT_GLOW_ALPHA;
            _runtimeMaterial.SetColor(GlowColorId, glowColor);
            _text.SetMaterialDirty();
        }

        private bool EnsureRuntimeMaterial()
        {
            if (_runtimeMaterial != null)
            {
                return true;
            }

            _text ??= GetComponent<TMP_Text>();
            if (_text == null)
            {
                return false;
            }

            Material sourceMaterial = _text.fontSharedMaterial;
            if (sourceMaterial == null || !sourceMaterial.HasProperty(GlowColorId))
            {
                return false;
            }

            _originalMaterial = sourceMaterial;
            _runtimeMaterial = new Material(sourceMaterial)
            {
                name = $"{sourceMaterial.name} ({gameObject.name})"
            };

            _text.fontSharedMaterial = _runtimeMaterial;
            return true;
        }

        private void RestoreOriginalMaterial()
        {
            if (_text != null && _originalMaterial != null)
            {
                _text.fontSharedMaterial = _originalMaterial;
            }
        }

        private void DestroyRuntimeMaterial()
        {
            if (_runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_runtimeMaterial);
            }
            else
            {
                DestroyImmediate(_runtimeMaterial);
            }

            _runtimeMaterial = null;
        }
    }
}
