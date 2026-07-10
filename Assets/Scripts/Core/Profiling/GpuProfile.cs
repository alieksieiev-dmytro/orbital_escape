using Newtonsoft.Json;

namespace OrbitalEscape.Profiling
{
    public sealed class GpuProfile
    {
        [JsonProperty("gpuFrameAvgMs")]
        public double? GpuFrameAvgMs { get; set; }

        [JsonProperty("gpuFrameMaxMs")]
        public double? GpuFrameMaxMs { get; set; }
    }
}
