using OrbitalEscape.Events;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    public class ObstacleCollider : MonoBehaviour
    {
        [SerializeField] private ArcObjectType _obstacleType;

        private void OnTriggerEnter2D(Collider2D other)
        {
            EventBus<OnObstacleCollidedEvent>.Raise(new OnObstacleCollidedEvent(_obstacleType, true));
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            EventBus<OnObstacleCollidedEvent>.Raise(new OnObstacleCollidedEvent(_obstacleType, false));
        }
    }
}
