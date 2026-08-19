using System;

namespace OrbitalEscape.Visuals
{
    public interface IPlayerDeathAnimation
    {
        void Play(Action onCompleted);
    }
}
