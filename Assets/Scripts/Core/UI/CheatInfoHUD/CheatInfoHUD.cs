using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OrbitalEscape.UI
{
    public sealed class CheatInfoHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetLines(IReadOnlyList<string> lines)
        {
            if (_text == null)
            {
                _text = GetComponentInChildren<TMP_Text>(true);
            }

            if (_text == null)
            {
                return;
            }

            _text.text = lines == null || lines.Count == 0
                ? string.Empty
                : string.Join("\n", lines);
        }
    }
}
