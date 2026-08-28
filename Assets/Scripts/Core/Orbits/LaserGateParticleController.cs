using UnityEngine;
using UnityEngine.Serialization;

namespace OrbitalEscape.Orbits
{
    [DisallowMultipleComponent]
    public sealed class LaserGateParticleController : MonoBehaviour
    {
        [FormerlySerializedAs("_particles")]
        [SerializeField] private ParticleSystem[] _passParticles;
        [FormerlySerializedAs("_chargingParticles")]
        [SerializeField] private ParticleSystem[] _pulseParticles;
        [SerializeField] private ParticleSystem _beamSparksParticles;

        [Header("Pass State")]
        [SerializeField] private Vector2 _velocityYRange = new(0.04f, 0.09f);
        [SerializeField] private Vector2 _startLifetimeRange = new(0.8f, 1.6f);

        private ParticleSystem.MinMaxCurve[] _chargingSizeCurves;
        private ParticleSystem.MinMaxCurve[] _activeSizeCurves;

        private void Reset()
        {
            ResolveReferences();
        }

        public void ApplyState(LaserGateState state)
        {
            ResolveReferences();
            CachePulseSizeCurves();
            ApplyPulseSizeCurves(state);
            SetPulseParticlesVisible(
                state == LaserGateState.Charging || state == LaserGateState.Active);
            SetBeamSparksEmission(state == LaserGateState.Active);

            if (state == LaserGateState.Pass)
            {
                ApplyPassSettings();
            }
        }

        public void Fit(float centerYOffset)
        {
            if (_beamSparksParticles == null)
            {
                return;
            }

            Transform sparksTransform = _beamSparksParticles.transform;
            sparksTransform.localPosition = new Vector3(0.0f, centerYOffset, 0.0f);
            sparksTransform.localScale = Vector3.one;
        }

        private void ApplyPassSettings()
        {
            foreach (ParticleSystem particles in _passParticles)
            {
                if (particles == null)
                {
                    continue;
                }

                ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
                velocity.enabled = true;
                velocity.y = CreateRange(_velocityYRange);

                ParticleSystem.MainModule main = particles.main;
                main.startLifetime = CreateRange(_startLifetimeRange);
            }
        }

        private void SetPulseParticlesVisible(bool visible)
        {
            foreach (ParticleSystem particles in _pulseParticles)
            {
                if (particles == null)
                {
                    continue;
                }

                if (!particles.isPlaying)
                {
                    particles.Play(true);
                }

                ParticleSystemRenderer particleRenderer =
                    particles.GetComponent<ParticleSystemRenderer>();

                if (particleRenderer != null)
                {
                    particleRenderer.enabled = visible;
                }
            }
        }

        private void SetBeamSparksEmission(bool active)
        {
            if (_beamSparksParticles == null)
            {
                return;
            }

            if (active)
            {
                if (!_beamSparksParticles.isEmitting)
                {
                    _beamSparksParticles.Play(true);
                }

                return;
            }

            if (_beamSparksParticles.isEmitting)
            {
                _beamSparksParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void CachePulseSizeCurves()
        {
            if (_chargingSizeCurves != null &&
                _chargingSizeCurves.Length == _pulseParticles.Length)
            {
                return;
            }

            _chargingSizeCurves = new ParticleSystem.MinMaxCurve[_pulseParticles.Length];
            _activeSizeCurves = new ParticleSystem.MinMaxCurve[_pulseParticles.Length];

            for (int i = 0; i < _pulseParticles.Length; i++)
            {
                ParticleSystem particles = _pulseParticles[i];
                if (particles == null)
                {
                    continue;
                }

                ParticleSystem.MinMaxCurve chargingCurve = CloneCurve(
                    particles.sizeOverLifetime.size);

                _chargingSizeCurves[i] = chargingCurve;
                _activeSizeCurves[i] = ReverseCurve(chargingCurve);
            }
        }

        private void ApplyPulseSizeCurves(LaserGateState state)
        {
            ParticleSystem.MinMaxCurve[] curves = state == LaserGateState.Active
                ? _activeSizeCurves
                : _chargingSizeCurves;

            for (int i = 0; i < _pulseParticles.Length; i++)
            {
                ParticleSystem particles = _pulseParticles[i];
                if (particles == null)
                {
                    continue;
                }

                ParticleSystem.SizeOverLifetimeModule sizeOverLifetime =
                    particles.sizeOverLifetime;

                sizeOverLifetime.size = CloneCurve(curves[i]);
            }
        }

        private void ResolveReferences()
        {
            if (_passParticles == null)
            {
                _passParticles = System.Array.Empty<ParticleSystem>();
            }

            if (_pulseParticles == null)
            {
                _pulseParticles = System.Array.Empty<ParticleSystem>();
            }
        }

        private static ParticleSystem.MinMaxCurve CreateRange(Vector2 range)
        {
            return new ParticleSystem.MinMaxCurve(
                Mathf.Min(range.x, range.y),
                Mathf.Max(range.x, range.y));
        }

        private static ParticleSystem.MinMaxCurve ReverseCurve(
            ParticleSystem.MinMaxCurve source)
        {
            return source.mode switch
            {
                ParticleSystemCurveMode.Curve => new ParticleSystem.MinMaxCurve(
                    source.curveMultiplier,
                    ReverseAnimationCurve(source.curve)),
                ParticleSystemCurveMode.TwoCurves => new ParticleSystem.MinMaxCurve(
                    source.curveMultiplier,
                    ReverseAnimationCurve(source.curveMin),
                    ReverseAnimationCurve(source.curveMax)),
                _ => source
            };
        }

        private static ParticleSystem.MinMaxCurve CloneCurve(
            ParticleSystem.MinMaxCurve source)
        {
            return source.mode switch
            {
                ParticleSystemCurveMode.Constant =>
                    new ParticleSystem.MinMaxCurve(source.constant),
                ParticleSystemCurveMode.Curve => new ParticleSystem.MinMaxCurve(
                    source.curveMultiplier,
                    CloneAnimationCurve(source.curve)),
                ParticleSystemCurveMode.TwoConstants =>
                    new ParticleSystem.MinMaxCurve(
                        source.constantMin,
                        source.constantMax),
                ParticleSystemCurveMode.TwoCurves => new ParticleSystem.MinMaxCurve(
                    source.curveMultiplier,
                    CloneAnimationCurve(source.curveMin),
                    CloneAnimationCurve(source.curveMax)),
                _ => source
            };
        }

        private static AnimationCurve CloneAnimationCurve(AnimationCurve source)
        {
            if (source == null)
            {
                return null;
            }

            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        private static AnimationCurve ReverseAnimationCurve(AnimationCurve source)
        {
            if (source == null)
            {
                return null;
            }

            Keyframe[] sourceKeys = source.keys;
            Keyframe[] reversedKeys = new Keyframe[sourceKeys.Length];

            for (int i = 0; i < sourceKeys.Length; i++)
            {
                Keyframe sourceKey = sourceKeys[sourceKeys.Length - 1 - i];
                Keyframe reversedKey = sourceKey;
                reversedKey.time = 1.0f - sourceKey.time;
                reversedKey.inTangent = -sourceKey.outTangent;
                reversedKey.outTangent = -sourceKey.inTangent;
                reversedKey.inWeight = sourceKey.outWeight;
                reversedKey.outWeight = sourceKey.inWeight;
                reversedKey.weightedMode = ReverseWeightedMode(sourceKey.weightedMode);
                reversedKeys[i] = reversedKey;
            }

            return new AnimationCurve(reversedKeys)
            {
                preWrapMode = source.postWrapMode,
                postWrapMode = source.preWrapMode
            };
        }

        private static WeightedMode ReverseWeightedMode(WeightedMode source)
        {
            WeightedMode reversed = WeightedMode.None;

            if ((source & WeightedMode.In) != 0)
            {
                reversed |= WeightedMode.Out;
            }

            if ((source & WeightedMode.Out) != 0)
            {
                reversed |= WeightedMode.In;
            }

            return reversed;
        }
    }
}
