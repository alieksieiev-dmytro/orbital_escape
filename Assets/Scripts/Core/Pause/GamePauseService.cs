using System;
using UnityEngine;

namespace OrbitalEscape.Pause
{
    public sealed class GamePauseService : IGamePauseService, IDisposable
    {
        private float _previousTimeScale = 1f;
        
        public bool IsPaused { get; private set; }

        public void Pause()
        {
            if (IsPaused)
            {
                return;
            }

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            IsPaused = true;
        }

        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
            IsPaused = false;
        }

        public void Toggle()
        {
            if (IsPaused)
            {
                Resume();
                return;
            }

            Pause();
        }

        public void Dispose()
        {
            Resume();
        }
    }
}
