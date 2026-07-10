using Newtonsoft.Json;

namespace OrbitalEscape.Profiling
{
    public sealed class FrameTimeProfile
    {
        [JsonProperty("avgMs")]
        public double? AvgMs { get; set; }

        [JsonProperty("minMs")]
        public double? MinMs { get; set; }

        [JsonProperty("maxMs")]
        public double? MaxMs { get; set; }

        [JsonProperty("p95Ms")]
        public double? P95Ms { get; set; }

        [JsonProperty("p99Ms")]
        public double? P99Ms { get; set; }

        [JsonProperty("fpsAvg")]
        public double? FpsAvg { get; set; }

        [JsonProperty("fps1PercentLow")]
        public double? Fps1PercentLow { get; set; }

        [JsonProperty("fps0_1PercentLow")]
        public double? Fps0_1PercentLow { get; set; }
    }
}
