using Newtonsoft.Json;

namespace OrbitalEscape.Profiling
{
    public sealed class CpuProfile
    {
        [JsonProperty("mainThreadAvgMs")]
        public double? MainThreadAvgMs { get; set; }

        [JsonProperty("renderThreadAvgMs")]
        public double? RenderThreadAvgMs { get; set; }

        [JsonProperty("gcAllocAvgBytes")]
        public double? GcAllocAvgBytes { get; set; }

        [JsonProperty("gcAllocTotalBytes")]
        public long? GcAllocTotalBytes { get; set; }

        [JsonProperty("gcCollectionCount")]
        public int? GcCollectionCount { get; set; }

        [JsonProperty("gcCollectionTotalMs")]
        public double? GcCollectionTotalMs { get; set; }
    }
}
