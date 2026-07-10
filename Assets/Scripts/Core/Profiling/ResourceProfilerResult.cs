namespace OrbitalEscape.Profiling
{
    public sealed class ResourceProfilerResult
    {
        private ResourceProfilerResult(bool success, string message, string reportPath)
        {
            Success = success;
            Message = message;
            ReportPath = reportPath;
        }

        public bool Success { get; }
        public string Message { get; }
        public string ReportPath { get; }

        public static ResourceProfilerResult Succeed(string message, string reportPath = null)
        {
            return new ResourceProfilerResult(true, message, reportPath);
        }

        public static ResourceProfilerResult Fail(string message)
        {
            return new ResourceProfilerResult(false, message, null);
        }
    }
}
