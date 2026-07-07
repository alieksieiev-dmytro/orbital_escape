using System;
using UnityEngine;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [Serializable]
    public struct PatternSlot
    {
        public const float DefaultSpaceLength = 1.0f;
        public const float MinimumSpaceLength = 0.01f;

        [SerializeField] private PatternSlotType _type;
        [SerializeField, Min(MinimumSpaceLength)] private float _length;

        public PatternSlotType Type => _type;
        public float Length => _length > 0.0f
            ? Mathf.Max(MinimumSpaceLength, _length)
            : DefaultSpaceLength;

        public float ResolveLength(PatternSlotLengthProvider lengthProvider)
        {
            return _type == PatternSlotType.Space
                ? Length
                : lengthProvider.GetObstacleLength(_type);
        }
    }
}
