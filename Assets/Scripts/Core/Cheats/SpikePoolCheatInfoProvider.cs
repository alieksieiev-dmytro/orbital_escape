using OrbitalEscape.Factories;
using Zenject;

namespace OrbitalEscape.Cheats
{
    public sealed class SpikePoolCheatInfoProvider : ICheatInfoProvider
    {
        private readonly ISpikeFactory _spikeFactory;

        public int Order => 20;

        [Inject]
        public SpikePoolCheatInfoProvider(ISpikeFactory spikeFactory)
        {
            _spikeFactory = spikeFactory;
        }

        public string GetLine()
        {
            return $"{_spikeFactory.CountAll} Spikes";
        }
    }
}
