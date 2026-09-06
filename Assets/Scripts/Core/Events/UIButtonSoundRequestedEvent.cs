using OrbitalEscape.Audio;

namespace OrbitalEscape.Events
{
    public readonly struct UIButtonSoundRequestedEvent : IEvent
    {
        public SfxType Sound { get; }

        public UIButtonSoundRequestedEvent(SfxType sound)
        {
            Sound = sound;
        }
    }
}
