using System.Collections.Generic;
using OrbitalEscape.Orbits;
using UnityEngine;

namespace OrbitalEscape.Visuals
{
    [DisallowMultipleComponent]
    public sealed class AuroraRiftClearanceController : MonoBehaviour
    {
        private const int MaxRifts = 32;

        private static readonly int AuroraRiftCountId = Shader.PropertyToID("_AuroraRiftCount");
        private static readonly int AuroraRiftsId = Shader.PropertyToID("_AuroraRifts");
        private static readonly int AuroraRiftAlphaMultiplierId = Shader.PropertyToID("_AuroraRiftAlphaMultiplier");

        [SerializeField] private SpriteRenderer _innerAurora;
        [SerializeField] private SpriteRenderer _outerAurora;

        private readonly Vector4[] _riftsPacked = new Vector4[MaxRifts];
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        public void Apply(IReadOnlyList<ArcObject> rifts, float alphaMultiplier)
        {
            int count = PackRifts(rifts);
            alphaMultiplier = Mathf.Clamp01(alphaMultiplier);

            ApplyToRenderer(_innerAurora, count, alphaMultiplier);
            ApplyToRenderer(_outerAurora, count, alphaMultiplier);
        }

        private int PackRifts(IReadOnlyList<ArcObject> rifts)
        {
            int count = 0;

            for (int i = 0; rifts != null && i < rifts.Count && count < MaxRifts; i++)
            {
                ArcObject rift = rifts[i];
                if (Equals(rift.Arc, default(Arc)) || rift.Arc.Radius <= 0.0f || !(rift.Object is Rift riftData))
                {
                    continue;
                }

                float visibleLength = Mathf.Max(0.0f, rift.Arc.Length() - riftData.Padding);
                if (visibleLength <= 0.0f)
                {
                    continue;
                }

                float angleShift = (riftData.Padding * 0.5f) / rift.Arc.Radius * Mathf.Rad2Deg;
                float startRadians = Mathf.Deg2Rad * Mathf.Repeat(rift.Arc.StartAngleDegree + angleShift, 360.0f);
                float deltaRadians = visibleLength / rift.Arc.Radius;
                _riftsPacked[count] = new Vector4(startRadians, deltaRadians, 0.0f, 1.0f);
                count++;
            }

            for (int i = count; i < MaxRifts; i++)
            {
                _riftsPacked[i] = Vector4.zero;
            }

            return count;
        }

        private void ApplyToRenderer(Renderer targetRenderer, int riftCount, float alphaMultiplier)
        {
            if (targetRenderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetInt(AuroraRiftCountId, riftCount);
            _propertyBlock.SetVectorArray(AuroraRiftsId, _riftsPacked);
            _propertyBlock.SetFloat(AuroraRiftAlphaMultiplierId, alphaMultiplier);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
