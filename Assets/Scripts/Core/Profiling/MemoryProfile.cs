using Newtonsoft.Json;

namespace OrbitalEscape.Profiling
{
    public sealed class MemoryProfile
    {
        [JsonProperty("totalReservedAvgMBytes")]
        public double? TotalReservedAvgMBytes { get; set; }

        [JsonProperty("totalUsedAvgMBytes")]
        public double? TotalUsedAvgMBytes { get; set; }

        [JsonProperty("managedHeapUsedAvgMBytes")]
        public double? ManagedHeapUsedAvgMBytes { get; set; }

        [JsonProperty("textureMemoryAvgMBytes")]
        public double? TextureMemoryAvgMBytes { get; set; }

        [JsonProperty("meshMemoryAvgMBytes")]
        public double? MeshMemoryAvgMBytes { get; set; }

        [JsonProperty("gcHeapReservedAvgMBytes")]
        public double? GcHeapReservedAvgMBytes { get; set; }
    }
}
