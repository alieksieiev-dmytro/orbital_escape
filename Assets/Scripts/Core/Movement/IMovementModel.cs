using UnityEngine;

namespace OrbitalEscape.Movement
{
    public interface IMovementModel
    {
        float Radius { get; set; }
        float AngleRadians { get; }
        void Reset(float angleRadians, float radius);
        
        void Step(float linearSpeedUnitsPerSec, float deltaTime, MovementDirection direction);
        Vector2 GetPosition(Vector2 center);
    }
}
