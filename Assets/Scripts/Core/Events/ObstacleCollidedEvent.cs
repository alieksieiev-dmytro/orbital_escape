using OrbitalEscape.Orbits;

namespace OrbitalEscape.Events
{
    public readonly struct ObstacleCollidedEvent : IEvent
    {
        public ArcObjectType ObstacleType { get; }
        public bool Entered { get; }
        
        public ObstacleCollidedEvent(ArcObjectType obstacleType, bool entered)
        {
            ObstacleType = obstacleType;
            Entered = entered;
        }
    }
}
