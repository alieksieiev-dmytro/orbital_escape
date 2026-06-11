using TMPro;
using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class PaletteTextColorTarget : MonoBehaviour, IPaletteTarget
    {
        [SerializeField] private TMP_Text _targetText;

        private void Reset()
        {
            ResolveText();
        }

        private void Awake()
        {
            ResolveText();
        }

        public void ApplyPalette(PaletteSnapshot palette)
        {
            if (_targetText == null)
            {
                ResolveText();
            }

            if (_targetText == null)
            {
                return;
            }

            Color color = palette.EnvironmentColor;
            color.a = _targetText.color.a;
            _targetText.color = color;
        }

        private void ResolveText()
        {
            if (_targetText == null)
            {
                _targetText = GetComponent<TMP_Text>();
            }
        }
    }
}
