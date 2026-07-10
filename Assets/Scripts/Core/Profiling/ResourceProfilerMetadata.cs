using System.Collections.Generic;
using Newtonsoft.Json;

namespace OrbitalEscape.Profiling
{
    public sealed class ResourceProfilerMetadata
    {
        [JsonProperty("schemaVersion")]
        public string SchemaVersion { get; set; }

        [JsonProperty("startedUtc")]
        public string StartedUtc { get; set; }

        [JsonProperty("endedUtc")]
        public string EndedUtc { get; set; }

        [JsonProperty("durationSeconds")]
        public double DurationSeconds { get; set; }

        [JsonProperty("frameCount")]
        public int FrameCount { get; set; }

        [JsonProperty("unityVersion")]
        public string UnityVersion { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("outputPath")]
        public string OutputPath { get; set; }

        [JsonProperty("warnings")]
        public IReadOnlyList<string> Warnings { get; set; }
    }
}
