using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OrbitalEscape.Cheats;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using Zenject;

namespace OrbitalEscape.Profiling
{
    public sealed class ResourceProfilerService : IResourceProfilerService, IInitializable, ITickable, IDisposable
    {
        private const string SchemaVersion = "1.0";
        private const double NanosecondsToMilliseconds = 0.000001;
        private const double BytesToMegabytes = 1.0 / (1024.0 * 1024.0);
        private const double MinimumSampleInterval = 0.1;
        private const int FrameTimingSampleCount = 1;

        private static readonly string[] MainThreadCounters = { "Main Thread" };
        private static readonly string[] RenderThreadCounters = { "Render Thread" };
        private static readonly string[] GcAllocCounters = { "GC Allocated In Frame", "GC.Alloc" };
        private static readonly string[] GcCollectCounters = { "GC.Collect", "GarbageCollectAssetsProfile" };
        private static readonly string[] TextureMemoryCounters = { "Texture Memory" };
        private static readonly string[] MeshMemoryCounters = { "Mesh Memory" };

        private readonly CheatPanelConfig _config;
        private readonly List<double> _frameTimesMs = new();
        private readonly List<string> _warnings = new();
        private readonly FrameTiming[] _frameTimings = new FrameTiming[FrameTimingSampleCount];
        private readonly DoubleSampleAccumulator _mainThreadFrameTimesMs = new();
        private readonly DoubleSampleAccumulator _renderThreadFrameTimesMs = new();
        private readonly DoubleSampleAccumulator _gpuFrameTimesMs = new();
        private readonly DoubleSampleAccumulator _gcAllocBytes = new();
        private readonly DoubleSampleAccumulator _gcCollectionMs = new();
        private readonly DoubleSampleAccumulator _totalReservedBytes = new();
        private readonly DoubleSampleAccumulator _totalUsedBytes = new();
        private readonly DoubleSampleAccumulator _managedHeapUsedBytes = new();
        private readonly DoubleSampleAccumulator _textureMemoryBytes = new();
        private readonly DoubleSampleAccumulator _meshMemoryBytes = new();
        private readonly DoubleSampleAccumulator _gcHeapReservedBytes = new();

        private ProfilerRecorder _mainThreadRecorder;
        private ProfilerRecorder _renderThreadRecorder;
        private ProfilerRecorder _gcAllocRecorder;
        private ProfilerRecorder _gcCollectRecorder;
        private ProfilerRecorder _textureMemoryRecorder;
        private ProfilerRecorder _meshMemoryRecorder;
        private DateTime _startedUtc;
        private int _startGcCollectionCount;
        private float _nextMemorySampleTime;

        [Inject]
        public ResourceProfilerService(CheatPanelConfig config)
        {
            _config = config;
        }

        public bool IsProfiling { get; private set; }
        public string LastReportPath { get; private set; }
        public event Action<bool> ProfilingStateChanged;

        public void Initialize()
        {
            Application.quitting += OnApplicationQuitting;
        }

        public void Tick()
        {
            if (!IsProfiling)
            {
                return;
            }

            SampleFrameTime();
            SampleFrameTimingManager();
            SampleProfilerRecorders();
            SampleMemoryOnInterval();
        }

        public void Dispose()
        {
            Application.quitting -= OnApplicationQuitting;

            if (IsProfiling)
            {
                StopProfiling();
            }

            DisposeRecorders();
        }

        public ResourceProfilerResult StartProfiling()
        {
            if (!_config.ShouldShowProfiling)
            {
                return ResourceProfilerResult.Fail("Profiling cheats are disabled.");
            }

            if (IsProfiling)
            {
                return ResourceProfilerResult.Fail("Profiling is already running.");
            }

            ResetSession();
            _startedUtc = DateTime.UtcNow;
            _startGcCollectionCount = GetTotalGcCollectionCount();
            _nextMemorySampleTime = Time.unscaledTime;

            _mainThreadRecorder = StartRecorder(ProfilerCategory.Internal, MainThreadCounters, "main thread CPU time");
            _renderThreadRecorder = StartRecorder(ProfilerCategory.Internal, RenderThreadCounters, "render thread CPU time");
            _gcAllocRecorder = StartRecorder(ProfilerCategory.Memory, GcAllocCounters, "GC allocation per frame");
            _gcCollectRecorder = StartRecorder(ProfilerCategory.Memory, GcCollectCounters, "GC collection time");
            _textureMemoryRecorder = StartRecorder(ProfilerCategory.Memory, TextureMemoryCounters, "texture memory");
            _meshMemoryRecorder = StartRecorder(ProfilerCategory.Memory, MeshMemoryCounters, "mesh memory");

            IsProfiling = true;
            ProfilingStateChanged?.Invoke(true);

            return ResourceProfilerResult.Succeed("Profiling started.");
        }

        public ResourceProfilerResult StopProfiling()
        {
            if (!IsProfiling)
            {
                return ResourceProfilerResult.Fail("Profiling is not running.");
            }

            DateTime endedUtc = DateTime.UtcNow;
            IsProfiling = false;
            ProfilingStateChanged?.Invoke(false);

            SampleMemory();
            ResourceProfilerReport report = BuildReport(endedUtc);
            string reportPath = CreateReportPath(endedUtc);
            LastReportPath = reportPath;
            report.Metadata.OutputPath = reportPath;
            WriteReport(report, reportPath);
            DisposeRecorders();

            return ResourceProfilerResult.Succeed($"Profiling saved: {Path.GetFileName(reportPath)}", reportPath);
        }

        private void OnApplicationQuitting()
        {
            if (IsProfiling)
            {
                StopProfiling();
            }
        }

        private void ResetSession()
        {
            DisposeRecorders();
            _frameTimesMs.Clear();
            _warnings.Clear();
            _mainThreadFrameTimesMs.Clear();
            _renderThreadFrameTimesMs.Clear();
            _gpuFrameTimesMs.Clear();
            _gcAllocBytes.Clear();
            _gcCollectionMs.Clear();
            _totalReservedBytes.Clear();
            _totalUsedBytes.Clear();
            _managedHeapUsedBytes.Clear();
            _textureMemoryBytes.Clear();
            _meshMemoryBytes.Clear();
            _gcHeapReservedBytes.Clear();
        }

        private void SampleFrameTime()
        {
            float deltaTime = Time.unscaledDeltaTime;

            if (deltaTime > 0f)
            {
                _frameTimesMs.Add(deltaTime * 1000.0);
            }
        }

        private void SampleFrameTimingManager()
        {
            FrameTimingManager.CaptureFrameTimings();
            uint timingCount = FrameTimingManager.GetLatestTimings(FrameTimingSampleCount, _frameTimings);

            if (timingCount == 0)
            {
                return;
            }

            FrameTiming timing = _frameTimings[0];
            _gpuFrameTimesMs.Add(timing.gpuFrameTime);

            if (!_mainThreadRecorder.Valid)
            {
                _mainThreadFrameTimesMs.Add(timing.cpuMainThreadFrameTime);
            }

            if (!_renderThreadRecorder.Valid)
            {
                _renderThreadFrameTimesMs.Add(timing.cpuRenderThreadFrameTime);
            }
        }

        private void SampleProfilerRecorders()
        {
            AddRecorderTimeSample(_mainThreadRecorder, _mainThreadFrameTimesMs);
            AddRecorderTimeSample(_renderThreadRecorder, _renderThreadFrameTimesMs);
            AddRecorderTimeSample(_gcCollectRecorder, _gcCollectionMs);
            AddRecorderByteSample(_gcAllocRecorder, _gcAllocBytes);
        }

        private void SampleMemoryOnInterval()
        {
            if (Time.unscaledTime < _nextMemorySampleTime)
            {
                return;
            }

            SampleMemory();
            float interval = Mathf.Max((float)MinimumSampleInterval, _config.ProfilerMemorySampleIntervalSeconds);
            _nextMemorySampleTime = Time.unscaledTime + interval;
        }

        private void SampleMemory()
        {
            _totalReservedBytes.Add(Profiler.GetTotalReservedMemoryLong());
            _totalUsedBytes.Add(Profiler.GetTotalAllocatedMemoryLong());
            _managedHeapUsedBytes.Add(Profiler.GetMonoUsedSizeLong());
            _gcHeapReservedBytes.Add(Profiler.GetMonoHeapSizeLong());
            AddRecorderByteSample(_textureMemoryRecorder, _textureMemoryBytes);
            AddRecorderByteSample(_meshMemoryRecorder, _meshMemoryBytes);
        }

        private ResourceProfilerReport BuildReport(DateTime endedUtc)
        {
            AddMissingSampleWarnings();

            return new ResourceProfilerReport
            {
                Metadata = new ResourceProfilerMetadata
                {
                    SchemaVersion = SchemaVersion,
                    StartedUtc = _startedUtc.ToString("O"),
                    EndedUtc = endedUtc.ToString("O"),
                    DurationSeconds = Math.Max(0.0, (endedUtc - _startedUtc).TotalSeconds),
                    FrameCount = _frameTimesMs.Count,
                    UnityVersion = Application.unityVersion,
                    Platform = Application.platform.ToString(),
                    OutputPath = null,
                    Warnings = _warnings.ToArray()
                },
                FrameTime = BuildFrameTimeProfile(),
                Cpu = new CpuProfile
                {
                    MainThreadAvgMs = _mainThreadFrameTimesMs.Average,
                    RenderThreadAvgMs = _renderThreadFrameTimesMs.Average,
                    GcAllocAvgBytes = _gcAllocBytes.Average,
                    GcAllocTotalBytes = _gcAllocBytes.Count > 0 ? (long?)Math.Round(GetAccumulatorSum(_gcAllocBytes)) : null,
                    GcCollectionCount = GetTotalGcCollectionCount() - _startGcCollectionCount,
                    GcCollectionTotalMs = _gcCollectionMs.Count > 0 ? GetAccumulatorSum(_gcCollectionMs) : null
                },
                Gpu = new GpuProfile
                {
                    GpuFrameAvgMs = _gpuFrameTimesMs.Average,
                    GpuFrameMaxMs = _gpuFrameTimesMs.Max
                },
                Memory = new MemoryProfile
                {
                    TotalReservedAvgMBytes = ToMegabytes(_totalReservedBytes.Average),
                    TotalUsedAvgMBytes = ToMegabytes(_totalUsedBytes.Average),
                    ManagedHeapUsedAvgMBytes = ToMegabytes(_managedHeapUsedBytes.Average),
                    TextureMemoryAvgMBytes = ToMegabytes(_textureMemoryBytes.Average),
                    MeshMemoryAvgMBytes = ToMegabytes(_meshMemoryBytes.Average),
                    GcHeapReservedAvgMBytes = ToMegabytes(_gcHeapReservedBytes.Average)
                }
            };
        }

        private FrameTimeProfile BuildFrameTimeProfile()
        {
            if (_frameTimesMs.Count == 0)
            {
                AddWarning("No frame time samples were collected.");
                return new FrameTimeProfile();
            }

            var sortedSamples = new List<double>(_frameTimesMs);
            sortedSamples.Sort();

            double averageMs = 0.0;
            double minMs = double.MaxValue;
            double maxMs = 0.0;

            for (int i = 0; i < _frameTimesMs.Count; i++)
            {
                double sample = _frameTimesMs[i];
                averageMs += sample;
                minMs = Math.Min(minMs, sample);
                maxMs = Math.Max(maxMs, sample);
            }

            averageMs /= _frameTimesMs.Count;
            double p95Ms = GetPercentile(sortedSamples, 0.95);
            double p99Ms = GetPercentile(sortedSamples, 0.99);
            double p999Ms = sortedSamples.Count >= 1000
                ? GetPercentile(sortedSamples, 0.999)
                : maxMs;

            if (sortedSamples.Count < 1000)
            {
                AddWarning("fps0_1PercentLow used max frame time because fewer than 1000 frame samples were collected.");
            }

            return new FrameTimeProfile
            {
                AvgMs = averageMs,
                MinMs = minMs,
                MaxMs = maxMs,
                P95Ms = p95Ms,
                P99Ms = p99Ms,
                FpsAvg = MillisecondsToFps(averageMs),
                Fps1PercentLow = MillisecondsToFps(p99Ms),
                Fps0_1PercentLow = MillisecondsToFps(p999Ms)
            };
        }

        private void WriteReport(ResourceProfilerReport report, string reportPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            string json = JsonConvert.SerializeObject(
                report,
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });

            File.WriteAllText(reportPath, json);
        }

        private string CreateReportPath(DateTime endedUtc)
        {
            string reportsDirectory = GetReportsDirectory();
            string filename = $"resource-profile_{endedUtc:yyyyMMdd_HHmmss}.json";
            return Path.Combine(reportsDirectory, filename);
        }

        private string GetReportsDirectory()
        {
            string folderName = string.IsNullOrWhiteSpace(_config.ProfilerReportsFolderName)
                ? "ProfilingReports"
                : _config.ProfilerReportsFolderName;

#if UNITY_EDITOR
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Path.Combine(projectRoot ?? Application.persistentDataPath, folderName);
#else
            return Path.Combine(Application.persistentDataPath, folderName);
#endif
        }

        private ProfilerRecorder StartRecorder(ProfilerCategory category, IReadOnlyList<string> counterNames, string metricName)
        {
            for (int i = 0; i < counterNames.Count; i++)
            {
                try
                {
                    ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, counterNames[i], 1);

                    if (recorder.Valid)
                    {
                        return recorder;
                    }

                    recorder.Dispose();
                }
                catch (Exception)
                {
                    // Counter names are platform and Unity-version dependent; missing counters are reported below.
                }
            }

            AddWarning($"Profiler counter unavailable: {metricName}.");
            return default;
        }

        private static void AddRecorderTimeSample(ProfilerRecorder recorder, DoubleSampleAccumulator accumulator)
        {
            if (recorder.Valid)
            {
                accumulator.Add(recorder.LastValueAsDouble * NanosecondsToMilliseconds);
            }
        }

        private static void AddRecorderByteSample(ProfilerRecorder recorder, DoubleSampleAccumulator accumulator)
        {
            if (recorder.Valid)
            {
                accumulator.Add(recorder.LastValueAsDouble);
            }
        }

        private void AddMissingSampleWarnings()
        {
            if (_mainThreadFrameTimesMs.Count == 0)
            {
                AddWarning("mainThreadAvgMs is unavailable.");
            }

            if (_renderThreadFrameTimesMs.Count == 0)
            {
                AddWarning("renderThreadAvgMs is unavailable.");
            }

            if (_gcAllocBytes.Count == 0)
            {
                AddWarning("GC allocation counters are unavailable.");
            }

            if (_gcCollectionMs.Count == 0)
            {
                AddWarning("gcCollectionTotalMs is unavailable; collection count is still reported.");
            }

            if (_gpuFrameTimesMs.Count == 0)
            {
                AddWarning("GPU frame timing is unavailable on this platform or graphics backend.");
            }

            if (_textureMemoryBytes.Count == 0)
            {
                AddWarning("textureMemoryAvgMBytes is unavailable.");
            }

            if (_meshMemoryBytes.Count == 0)
            {
                AddWarning("meshMemoryAvgMBytes is unavailable.");
            }
        }

        private void DisposeRecorders()
        {
            DisposeRecorder(ref _mainThreadRecorder);
            DisposeRecorder(ref _renderThreadRecorder);
            DisposeRecorder(ref _gcAllocRecorder);
            DisposeRecorder(ref _gcCollectRecorder);
            DisposeRecorder(ref _textureMemoryRecorder);
            DisposeRecorder(ref _meshMemoryRecorder);
        }

        private static void DisposeRecorder(ref ProfilerRecorder recorder)
        {
            if (recorder.Valid)
            {
                recorder.Dispose();
            }

            recorder = default;
        }

        private void AddWarning(string warning)
        {
            if (!_warnings.Contains(warning))
            {
                _warnings.Add(warning);
            }
        }

        private static double GetPercentile(IReadOnlyList<double> sortedSamples, double percentile)
        {
            if (sortedSamples.Count == 0)
            {
                return 0.0;
            }

            double rawIndex = (sortedSamples.Count - 1) * percentile;
            int lowerIndex = Mathf.FloorToInt((float)rawIndex);
            int upperIndex = Mathf.CeilToInt((float)rawIndex);

            if (lowerIndex == upperIndex)
            {
                return sortedSamples[lowerIndex];
            }

            double blend = rawIndex - lowerIndex;
            return sortedSamples[lowerIndex] + (sortedSamples[upperIndex] - sortedSamples[lowerIndex]) * blend;
        }

        private static double? ToMegabytes(double? bytes)
        {
            return bytes.HasValue ? bytes.Value * BytesToMegabytes : null;
        }

        private static double? MillisecondsToFps(double milliseconds)
        {
            return milliseconds > 0.0 ? 1000.0 / milliseconds : null;
        }

        private static int GetTotalGcCollectionCount()
        {
            return GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
        }

        private static double GetAccumulatorSum(DoubleSampleAccumulator accumulator)
        {
            return accumulator.Average.GetValueOrDefault() * accumulator.Count;
        }
    }
}
