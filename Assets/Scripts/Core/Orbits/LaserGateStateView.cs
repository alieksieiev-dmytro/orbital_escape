using UnityEngine;

namespace OrbitalEscape.Orbits
{
    [DisallowMultipleComponent]
    public sealed class LaserGateStateView : MonoBehaviour
    {
        private const float MinimumDimension = 0.01f;

        [SerializeField] private LaserGateState _state;
        [SerializeField] private Transform _beam;

        public LaserGateState State => _state;

        private void Reset()
        {
            ResolveReferences();
        }

        public void Fit(float width, float height, float centerYOffset)
        {
            ResolveReferences();

            float safeWidth = Mathf.Max(MinimumDimension, width);
            float safeHeight = Mathf.Max(MinimumDimension, height);

            if (_beam != null)
            {
                _beam.localPosition = new Vector3(0.0f, centerYOffset, 0.0f);
                _beam.localScale = new Vector3(safeWidth, safeHeight, 1.0f);
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        private void ResolveReferences()
        {
            if (_beam == null)
            {
                _beam = transform.Find("beam");
            }

        }
    }
}
