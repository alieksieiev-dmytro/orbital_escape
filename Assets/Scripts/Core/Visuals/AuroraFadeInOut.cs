using DG.Tweening;
using OrbitalEscape.Events;
using OrbitalEscape.Orbits;
using OrbitalEscape.Visuals.Palettes;
using UnityEngine;

namespace OrbitalEscape.Visuals
{
    public class AuroraFadeInOut : MonoBehaviour
    {
        private const float BLOOM_RADIUS = 2.5f;
        
        private static readonly int AuroraRingRadius = Shader.PropertyToID("_Radius");
        private static readonly int AuroraOuterColour = Shader.PropertyToID("_InnerColor");
        private static readonly int AuroraInnerColour = Shader.PropertyToID("_EdgeColor");
        
        [SerializeField] private SpriteRenderer _innerAurora;
        [SerializeField] private SpriteRenderer _outerAurora;

        private Orbit _data;
        private Tween _tween;
        private float _initialInnerAlpha;
        private float _initialOuterAlpha;
        private bool _initialInnerActive;
        private bool _initialOuterActive;
        private EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitEventBinding;

        private void Awake()
        {
            _initialInnerAlpha = _innerAurora.material.GetColor(AuroraInnerColour).a;
            _initialOuterAlpha = _outerAurora.material.GetColor(AuroraOuterColour).a;
            _initialInnerActive = _innerAurora.gameObject.activeSelf;
            _initialOuterActive = _outerAurora.gameObject.activeSelf;
            _passedThroughOrbitEventBinding = new EventBinding<PassedThroughOrbitEvent>(OnPassedThroughOrbitEvent);
        }

        private void OnEnable() => EventBus<PassedThroughOrbitEvent>.Register(_passedThroughOrbitEventBinding);
        
        private void OnDisable()
        {
            EventBus<PassedThroughOrbitEvent>.Deregister(_passedThroughOrbitEventBinding);
            _tween?.Kill();
            _tween = null;
        }

        public void ResetForRun(Orbit orbit)
        {
            _tween?.Kill();
            _tween = null;
            InitAurora(orbit);
            _innerAurora.gameObject.SetActive(_initialInnerActive);
            _outerAurora.gameObject.SetActive(_initialOuterActive);
            Color inner = _innerAurora.material.GetColor(AuroraInnerColour);
            Color outer = _outerAurora.material.GetColor(AuroraOuterColour);
            inner.a = _initialInnerAlpha;
            outer.a = _initialOuterAlpha;
            _innerAurora.material.SetColor(AuroraInnerColour, inner);
            _outerAurora.material.SetColor(AuroraOuterColour, outer);
        }

        public void InitAurora(Orbit orbit)
        {
            _data = orbit;
            float outerAuroraRadius = (orbit.Radius + BLOOM_RADIUS) / orbit.Radius;
            _outerAurora.transform.localScale = Vector3.one * outerAuroraRadius;
            _outerAurora.material.SetFloat(AuroraRingRadius, 1 / outerAuroraRadius);
        }
        
        public void SetActive(bool isOuter)
        {
            _outerAurora.gameObject.SetActive(isOuter);
            _innerAurora.gameObject.SetActive(!isOuter);
        }

        public void ApplyPalette(PaletteSnapshot palette)
        {
            SetMaterialColor(_innerAurora, AuroraInnerColour, palette.EnvironmentColor);
            SetMaterialColor(_outerAurora, AuroraOuterColour, palette.EnvironmentColor);
        }
        
        private void PlayFadeIn(float duration, bool playInner, bool playOuter)
        {
            var currentInnerColor = _innerAurora.material.GetColor(AuroraInnerColour);
            var currentOuterColor = _outerAurora.material.GetColor(AuroraOuterColour);
            currentOuterColor.a = 0;
            currentInnerColor.a = 0;

            _tween?.Complete();
            _tween = DOTween.To(() => currentOuterColor.a, x => {
                currentOuterColor.a = x;
                currentInnerColor.a = x;
                if (playOuter) _outerAurora.material.SetColor(AuroraOuterColour, currentOuterColor);
                if (playInner) _innerAurora.material.SetColor(AuroraInnerColour, currentInnerColor);
            }, 1f, duration).SetEase(Ease.InOutQuad);
        }
        
        private void PlayFadeOut(float duration, bool playInner, bool playOuter)
        {
            var currentInnerColor = _innerAurora.material.GetColor(AuroraInnerColour);
            var currentOuterColor = _outerAurora.material.GetColor(AuroraOuterColour);

            _tween?.Complete();
            _tween = DOTween.To(() => currentOuterColor.a, x => {
                currentOuterColor.a = x;
                currentInnerColor.a = x;
                if (playOuter) _outerAurora.material.SetColor(AuroraOuterColour, currentOuterColor);
                if (playInner) _innerAurora.material.SetColor(AuroraInnerColour, currentInnerColor);
            }, 0f, duration).SetEase(Ease.InOutQuad);
        }

        private void OnPassedThroughOrbitEvent(PassedThroughOrbitEvent @event)
        {
            if (@event.IsOuterOrbit)
            {
                if (@event.Orbit.Index == _data.Index + 1)
                {
                    PlayFadeOut(1f, true, true);
                }
                else if (@event.Orbit.Index == _data.Index)
                {
                    _innerAurora.gameObject.SetActive(true);
                    _outerAurora.gameObject.SetActive(true);
                    PlayFadeOut(1f, false, true);
                    PlayFadeIn(2f, true, false);
                }
                else if (@event.Orbit.Index == _data.Index - 1)
                {
                    _outerAurora.gameObject.SetActive(true);
                    PlayFadeIn(2f, false, true);
                }
            }
            else
            {
                if (@event.Orbit.Index == _data.Index - 1)
                {
                    PlayFadeOut(1f, true, true);
                }
                else if (@event.Orbit.Index == _data.Index)
                {
                    _innerAurora.gameObject.SetActive(true);
                    _outerAurora.gameObject.SetActive(true);
                    PlayFadeOut(1f, true, false);
                    PlayFadeIn(2f, false, true);
                }
                else if (@event.Orbit.Index == _data.Index + 1)
                {
                    _innerAurora.gameObject.SetActive(true);
                    PlayFadeIn(2f, true, false);
                }
            }
        }

        private void SetMaterialColor(SpriteRenderer target, int colorId, Color color)
        {
            if (target == null || target.material == null || !target.material.HasProperty(colorId))
            {
                return;
            }

            Color currentColor = target.material.GetColor(colorId);
            color.a = currentColor.a;
            target.material.SetColor(colorId, color);
        }
    }
}
