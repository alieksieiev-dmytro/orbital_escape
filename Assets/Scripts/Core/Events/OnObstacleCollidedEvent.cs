using OrbitalEscape.Orbits;

namespace OrbitalEscape.Events
{
    public readonly struct OnObstacleCollidedEvent : IEvent
    {
        public ArcObjectType ObstacleType { get; }
        public bool Entered { get; }
        
        public OnObstacleCollidedEvent(ArcObjectType obstacleType, bool entered)
        {
            ObstacleType = obstacleType;
            Entered = entered;
        }
    }
}
