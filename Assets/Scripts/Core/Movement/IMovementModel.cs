using UnityEngine;

namespace OrbitalEscape.Movement
{
    public interface IMovementModel
    {
        float Radius { get; set; }
        
        void Step(float linearSpeedUnitsPerSec, float deltaTime, MovementDirection direction);
        Vector2 GetPosition(Vector2 center);
    }
}
