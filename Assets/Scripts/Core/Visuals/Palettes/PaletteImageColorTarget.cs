using UnityEngine;
using UnityEngine.UI;

namespace OrbitalEscape.Visuals.Palettes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class PaletteImageColorTarget : MonoBehaviour, IPaletteTarget
    {
        [SerializeField] private Image _targetImage;
        [SerializeField] private PaletteColorType _colorType = PaletteColorType.Accent;
        [SerializeField] private bool _preserveCurrentAlpha = true;

        private void Reset()
        {
            ResolveImage();
        }

        private void Awake()
        {
            ResolveImage();
        }

        public void ApplyPalette(PaletteSnapshot palette)
        {
            ResolveImage();
            if (_targetImage == null)
            {
                return;
            }

            Color color = palette.GetColor(_colorType);
            if (_preserveCurrentAlpha)
            {
                color.a = _targetImage.color.a;
            }

            _targetImage.color = color;
        }

        private void ResolveImage()
        {
            _targetImage ??= GetComponent<Image>();
        }
    }
}
