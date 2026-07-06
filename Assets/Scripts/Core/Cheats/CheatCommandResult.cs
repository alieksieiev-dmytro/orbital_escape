namespace OrbitalEscape.Cheats
{
    public readonly struct CheatCommandResult
    {
        public bool Success { get; }
        public string Message { get; }

        public CheatCommandResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}
