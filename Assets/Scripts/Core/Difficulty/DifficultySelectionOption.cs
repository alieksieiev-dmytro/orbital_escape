using System;
using UnityEngine;

namespace OrbitalEscape.Difficulty
{
    [Serializable]
    public struct DifficultySelectionOption
    {
        [SerializeField] private DifficultyType _difficulty;
        [SerializeField] private bool _enabled;
        [SerializeField] private string _title;
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _image;

        public DifficultyType Difficulty => _difficulty;
        public bool Enabled => _enabled;
        public string Title => string.IsNullOrWhiteSpace(_title) ? GetDefaultTitle(_difficulty) : _title;
        public string Description => string.IsNullOrWhiteSpace(_description) ? GetDefaultDescription(_difficulty) : _description;
        public Sprite Image => _image;

        public static DifficultySelectionOption Default(DifficultyType difficulty)
        {
            return new DifficultySelectionOption
            {
                _difficulty = difficulty,
                _enabled = true,
                _title = GetDefaultTitle(difficulty),
                _description = GetDefaultDescription(difficulty)
            };
        }

        public DifficultySelectionOption WithDefaults()
        {
            return new DifficultySelectionOption
            {
                _difficulty = _difficulty,
                _enabled = _enabled,
                _title = Title,
                _description = Description,
                _image = _image
            };
        }

        private static string GetDefaultTitle(DifficultyType difficulty)
        {
            return difficulty.ToString().ToUpperInvariant();
        }

        private static string GetDefaultDescription(DifficultyType difficulty)
        {
            return difficulty switch
            {
                DifficultyType.Easy => "A readable route for learning the rhythm of orbit jumps.",
                DifficultyType.Medium => "Balanced pressure with denser obstacle chains.",
                DifficultyType.Hard => "Sharper patterns and faster escalation for confident runs.",
                DifficultyType.Impossible => "A hostile route tuned for precision and recovery.",
                DifficultyType.Nightmare => "Maximum pressure with very little room to breathe.",
                _ => "A custom difficulty route."
            };
        }
    }
}
