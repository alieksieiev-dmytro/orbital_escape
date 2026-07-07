using System;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public readonly struct PatternSlotLengthProvider
    {
        private readonly float _spikeLength;
        private readonly float _miniSpikeLength;
        private readonly float _impulseTrapLength;
        private readonly float _gravityWellLength;
        private readonly float _riftLength;
        private readonly float _laserGateLength;

        public PatternSlotLengthProvider(
            float spikeLength,
            float miniSpikeLength,
            float impulseTrapLength,
            float gravityWellLength,
            float riftLength,
            float laserGateLength)
        {
            _spikeLength = Math.Max(0.01f, spikeLength);
            _miniSpikeLength = Math.Max(0.01f, miniSpikeLength);
            _impulseTrapLength = Math.Max(0.01f, impulseTrapLength);
            _gravityWellLength = Math.Max(0.01f, gravityWellLength);
            _riftLength = Math.Max(0.01f, riftLength);
            _laserGateLength = Math.Max(0.01f, laserGateLength);
        }

        public float GetObstacleLength(PatternSlotType type)
        {
            return type switch
            {
                PatternSlotType.Spike => _spikeLength,
                PatternSlotType.MiniSpike => _miniSpikeLength,
                PatternSlotType.ImpulseTrap => _impulseTrapLength,
                PatternSlotType.GravityWell => _gravityWellLength,
                PatternSlotType.Rift => _riftLength,
                PatternSlotType.LaserGate => _laserGateLength,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    "Only obstacle slot lengths are provided by gameplay configs.")
            };
        }
    }
}
