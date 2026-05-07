using System;
using OrbitalEscape.Difficulty;
using UnityEngine;

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
                palette.MainGradient.Evaluate(progress),
                palette.OutlineGradient.Evaluate(progress),
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
            [SerializeField] private Gradient _mainGradient;
            [SerializeField] private Gradient _outlineGradient;

            public DifficultyType Difficulty => _difficulty;
            public Gradient MainGradient => _mainGradient ?? DefaultGradient(Color.white);
            public Gradient OutlineGradient => _outlineGradient ?? DefaultGradient(Color.white);

            public static DifficultyPalette Default => new DifficultyPalette
            {
                _difficulty = DifficultyType.Basic,
                _scoreToGradientMultiplier = 0.05f,
                _loop = true,
                _mainGradient = DefaultGradient(new Color(0.65f, 0.15f, 0.95f, 1.0f)),
                _outlineGradient = DefaultGradient(new Color(0.0f, 1.0f, 0.75f, 1.0f))
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
