using Newtonsoft.Json;

namespace OrbitalEscape.Profiling
{
    public sealed class ResourceProfilerReport
    {
        [JsonProperty("metadata")]
        public ResourceProfilerMetadata Metadata { get; set; }

        [JsonProperty("frameTime")]
        public FrameTimeProfile FrameTime { get; set; }

        [JsonProperty("cpu")]
        public CpuProfile Cpu { get; set; }

        [JsonProperty("gpu")]
        public GpuProfile Gpu { get; set; }

        [JsonProperty("memory")]
        public MemoryProfile Memory { get; set; }
    }
}
