namespace OrbitalEscape.Pause
{
    public interface IGamePauseService
    {
        bool IsPaused { get; }
        
        void Pause();
        void Resume();
        void Toggle();
    }
}
