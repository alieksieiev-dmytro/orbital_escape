using OrbitalEscape.Visuals.Palettes;
using UnityEngine;

namespace OrbitalEscape.Visuals
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GravityWellArcVisual : MonoBehaviour, IPaletteTarget
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int MinAlphaId = Shader.PropertyToID("_MinAlpha");
        private static readonly int MaxAlphaId = Shader.PropertyToID("_MaxAlpha");
        private static readonly int ArcCenterId = Shader.PropertyToID("_ArcCenter");
        private static readonly int ArcRadiusId = Shader.PropertyToID("_ArcRadius");
        private static readonly int ArcThicknessId = Shader.PropertyToID("_ArcThickness");
        private static readonly int ArcStartAngleId = Shader.PropertyToID("_ArcStartAngle");
        private static readonly int ArcAngleId = Shader.PropertyToID("_ArcAngle");
        private static readonly int ArcAngleSoftnessId = Shader.PropertyToID("_ArcAngleSoftness");
        private static readonly int ArcRadiusSoftnessId = Shader.PropertyToID("_ArcRadiusSoftness");
        private static readonly int PullDirectionId = Shader.PropertyToID("_PullDirection");

        [SerializeField] private SpriteRenderer _targetRenderer;
        [SerializeField, Min(0.0f)] private float _angleSoftnessRadians;
        [SerializeField, Min(0.0f)] private float _radiusSoftness;

        private MaterialPropertyBlock _propertyBlock;
        private Vector2 _arcCenter;
        private Color _color = Color.white;
        private float _arcRadius = 5.0f;
        private float _arcThickness = 1.0f;
        private float _arcStartAngleRadians;
        private float _arcAngleRadians = Mathf.PI * 0.5f;
        private float _minAlpha = 0.25f;
        private float _maxAlpha = 0.25f;
        private float _pullDirection = -1.0f;
        private bool _configured;

        private void Reset()
        {
            ResolveRenderer();
        }

        private void Awake()
        {
            ResolveRenderer();
            _propertyBlock ??= new MaterialPropertyBlock();
            if (!_configured)
            {
                Apply();
            }
        }

        public void Configure(
            Vector2 arcCenter,
            float arcRadius,
            float arcThickness,
            float arcStartAngleRadians,
            float arcAngleRadians,
            float maxAlpha,
            bool pullsToUpperOrbit)
        {
            ResolveRenderer();
            _propertyBlock ??= new MaterialPropertyBlock();

            _arcCenter = arcCenter;
            _arcRadius = Mathf.Max(0.0f, arcRadius);
            _arcThickness = Mathf.Max(0.0f, arcThickness);
            _arcStartAngleRadians = arcStartAngleRadians;
            _arcAngleRadians = Mathf.Max(0.0f, arcAngleRadians);
            _maxAlpha = Mathf.Clamp01(maxAlpha);
            _minAlpha = Mathf.Min(GetMaterialMinAlpha(), _maxAlpha);
            _pullDirection = pullsToUpperOrbit ? 1.0f : -1.0f;
            _configured = true;

            Apply();
        }

        public void ApplyPalette(PaletteSnapshot palette)
        {
            _color = palette.OutlineColor;
            _color.a = 1.0f;
            Apply();
        }

        private float GetMaterialMinAlpha()
        {
            Material material = _targetRenderer != null ? _targetRenderer.sharedMaterial : null;
            if (material == null || !material.HasProperty(MinAlphaId))
            {
                return _maxAlpha;
            }

            return Mathf.Clamp01(material.GetFloat(MinAlphaId));
        }

        private void Apply()
        {
            if (_targetRenderer == null)
            {
                ResolveRenderer();
            }

            if (_targetRenderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _targetRenderer.color = Color.white;
            _targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(ColorId, _color);
            _propertyBlock.SetFloat(MinAlphaId, _minAlpha);
            _propertyBlock.SetFloat(MaxAlphaId, _maxAlpha);
            _propertyBlock.SetVector(ArcCenterId, new Vector4(_arcCenter.x, _arcCenter.y, 0.0f, 0.0f));
            _propertyBlock.SetFloat(ArcRadiusId, _arcRadius);
            _propertyBlock.SetFloat(ArcThicknessId, _arcThickness);
            _propertyBlock.SetFloat(ArcStartAngleId, _arcStartAngleRadians);
            _propertyBlock.SetFloat(ArcAngleId, _arcAngleRadians);
            _propertyBlock.SetFloat(ArcAngleSoftnessId, _angleSoftnessRadians);
            _propertyBlock.SetFloat(ArcRadiusSoftnessId, _radiusSoftness);
            _propertyBlock.SetFloat(PullDirectionId, _pullDirection);
            _targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void ResolveRenderer()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<SpriteRenderer>();
            }
        }
    }
}
