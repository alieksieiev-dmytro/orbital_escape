using OrbitalEscape.Movement;

namespace OrbitalEscape.Events
{
    public readonly struct MovementDirectionChangedEvent : IEvent
    {
        public MovementDirection Direction { get; }

        public MovementDirectionChangedEvent(MovementDirection direction)
        {
            Direction = direction;
        }
    }
}
