using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitalEscape.Difficulty
{
    [CreateAssetMenu(fileName = "DifficultySelectionConfig", menuName = "Scriptable Objects/Configs/Difficulty Selection Config")]
    public sealed class DifficultySelectionConfig : ScriptableObject
    {
        [SerializeField] private DifficultySelectionOption[] _options;

        public IReadOnlyList<DifficultySelectionOption> CreateOptions()
        {
            var options = new List<DifficultySelectionOption>();
            var configuredDifficulties = new HashSet<DifficultyType>();

            if (_options != null)
            {
                for (int i = 0; i < _options.Length; i++)
                {
                    DifficultySelectionOption option = _options[i].WithDefaults();
                    configuredDifficulties.Add(option.Difficulty);

                    if (option.Enabled)
                    {
                        options.Add(option);
                    }
                }
            }

            foreach (DifficultyType difficulty in Enum.GetValues(typeof(DifficultyType)))
            {
                if (!configuredDifficulties.Contains(difficulty))
                {
                    options.Add(DifficultySelectionOption.Default(difficulty));
                }
            }

            if (options.Count == 0)
            {
                options.Add(DifficultySelectionOption.Default(DifficultyType.Easy));
            }

            return options;
        }
    }
}
