using System;
using UnityEngine;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [Serializable]
    public struct PatternSlot
    {
        public const float DefaultSpaceLength = 1.0f;
        public const float MinimumSpaceLength = 0.01f;
        public const float DefaultSpikeHeight = 0.95f;
        public const float MinimumSpikeHeight = 0.01f;
        public const float DefaultRandomizedSpikeMaxHeight = 1.2f;

        [SerializeField] private PatternSlotType _type;
        [SerializeField, Min(MinimumSpaceLength)] private float _length;
        [SerializeField, Min(MinimumSpikeHeight)] private float _height;
        [SerializeField, Min(MinimumSpikeHeight)] private float _minHeight;
        [SerializeField, Min(MinimumSpikeHeight)] private float _maxHeight;

        public PatternSlotType Type => _type;
        public float Length => _length > 0.0f
            ? Mathf.Max(MinimumSpaceLength, _length)
            : DefaultSpaceLength;
        public float Height => _height > 0.0f
            ? Mathf.Max(MinimumSpikeHeight, _height)
            : DefaultSpikeHeight;
        public float MinHeight => _minHeight > 0.0f
            ? Mathf.Max(MinimumSpikeHeight, _minHeight)
            : DefaultSpikeHeight;
        public float MaxHeight => Mathf.Max(
            MinHeight,
            _maxHeight > 0.0f ? _maxHeight : DefaultRandomizedSpikeMaxHeight);

        public float ResolveLength(PatternSlotLengthProvider lengthProvider)
        {
            return _type == PatternSlotType.Space
                ? Length
                : lengthProvider.GetObstacleLength(_type);
        }
    }
}
