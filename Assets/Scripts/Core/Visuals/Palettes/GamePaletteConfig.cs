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
    }
}
