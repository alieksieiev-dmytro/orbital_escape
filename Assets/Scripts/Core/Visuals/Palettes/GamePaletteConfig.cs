using System;
using OrbitalEscape.Difficulty;
using UnityEngine;
using UnityEngine.Serialization;

namespace OrbitalEscape.Visuals.Palettes
{
    [CreateAssetMenu(fileName = "GamePaletteConfig", menuName = "Scriptable Objects/Configs/Game Palette Config")]
    public sealed class GamePaletteConfig : ScriptableObject
    {
        [SerializeField] private DifficultyPalette[] _difficultyPalettes;

        public PaletteSnapshot Evaluate(DifficultyType difficulty, float score)
        {
            DifficultyPalette palette = GetPalette(difficulty);
            float progress = palette.EvaluateProgress(score);
            return new PaletteSnapshot(
                palette.BackgroundGradient.Evaluate(progress),
                palette.EnvironmentGradient.Evaluate(progress),
                palette.AccentGradient.Evaluate(progress),
                progress);
        }

        private DifficultyPalette GetPalette(DifficultyType difficulty)
        {
            if (_difficultyPalettes != null)
            {
                for (int i = 0; i < _difficultyPalettes.Length; i++)
                {
                    if (_difficultyPalettes[i].Difficulty == difficulty)
                    {
                        return _difficultyPalettes[i];
                    }
                }

                if (_difficultyPalettes.Length > 0)
                {
                    return _difficultyPalettes[0];
                }
            }

            return DifficultyPalette.Default;
        }

        [Serializable]
        public struct DifficultyPalette
        {
            [SerializeField] private DifficultyType _difficulty;
            [SerializeField, Min(0.0f)] private float _scoreToGradientMultiplier;
            [SerializeField] private bool _loop;
            [SerializeField, FormerlySerializedAs("_mainGradient")] private Gradient _backgroundGradient;
            [SerializeField] private Gradient _environmentGradient;
            [SerializeField, FormerlySerializedAs("_outlineGradient")] private Gradient _accentGradient;

            public DifficultyType Difficulty => _difficulty;
            public Gradient BackgroundGradient => _backgroundGradient ?? DefaultGradient(Color.black);
            public Gradient EnvironmentGradient => _environmentGradient ?? DefaultGradient(new Color(0.05f, 0.9f, 1.0f, 1.0f));
            public Gradient AccentGradient => _accentGradient ?? DefaultGradient(Color.white);

            public static DifficultyPalette Default => new DifficultyPalette
            {
                _difficulty = DifficultyType.Easy,
                _scoreToGradientMultiplier = 0.05f,
                _loop = true,
                _backgroundGradient = DefaultGradient(new Color(0.02f, 0.0f, 0.08f, 1.0f)),
                _environmentGradient = DefaultGradient(new Color(0.05f, 0.9f, 1.0f, 1.0f)),
                _accentGradient = DefaultGradient(new Color(0.0f, 1.0f, 0.75f, 1.0f))
            };

            public float EvaluateProgress(float score)
            {
                float rawProgress = Mathf.Max(0.0f, score) * _scoreToGradientMultiplier;
                return _loop ? Mathf.Repeat(rawProgress, 1.0f) : Mathf.Clamp01(rawProgress);
            }

            private static Gradient DefaultGradient(Color color)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
                    new[] { new GradientAlphaKey(color.a, 0.0f), new GradientAlphaKey(color.a, 1.0f) });
                return gradient;
            }
        }
    }
}
