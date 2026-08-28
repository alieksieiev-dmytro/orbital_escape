using UnityEngine;

namespace OrbitalEscape.Orbits
{
    [DisallowMultipleComponent]
    public sealed class LaserGateBaseView : MonoBehaviour
    {
        private const float MinimumDimension = 0.01f;

        [SerializeField] private SpriteRenderer _lowerBase;
        [SerializeField] private SpriteRenderer _upperBase;

        private void Reset()
        {
            ResolveReferences();
        }

        public void Fit(float width, float height, float centerYOffset)
        {
            ResolveReferences();

            float safeWidth = Mathf.Max(MinimumDimension, width);
            float safeHeight = Mathf.Max(MinimumDimension, height);

            FitBase(_lowerBase, centerYOffset - safeHeight * 0.5f, safeWidth);
            FitBase(_upperBase, centerYOffset + safeHeight * 0.5f, safeWidth);
        }

        private static void FitBase(SpriteRenderer sprite, float heightOffset, float width)
        {
            if (sprite == null)
            {
                return;
            }

            sprite.transform.localPosition = new Vector3(0.0f, heightOffset, 0.0f);
            sprite.transform.localRotation = Quaternion.identity;

            float spriteWidth = sprite.sprite != null
                ? sprite.sprite.bounds.size.x
                : 1.0f;
            Vector3 scale = sprite.transform.localScale;
            scale.x = width / Mathf.Max(MinimumDimension, spriteWidth);
            sprite.transform.localScale = scale;
        }

        private void ResolveReferences()
        {
            if (_lowerBase == null)
            {
                Transform lowerBase = transform.Find("base_lower");
                if (lowerBase != null)
                {
                    _lowerBase = lowerBase.GetComponent<SpriteRenderer>();
                }
            }

            if (_upperBase == null)
            {
                Transform upperBase = transform.Find("base_upper");
                if (upperBase != null)
                {
                    _upperBase = upperBase.GetComponent<SpriteRenderer>();
                }
            }
        }
    }
}
