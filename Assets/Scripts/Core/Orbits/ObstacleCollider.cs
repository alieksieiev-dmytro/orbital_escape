using OrbitalEscape.Events;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    public class ObstacleCollider : MonoBehaviour
    {
        [SerializeField] private ArcObjectType _obstacleType;

        private void OnTriggerEnter2D(Collider2D other)
        {
            EventBus<ObstacleCollidedEvent>.Raise(new ObstacleCollidedEvent(_obstacleType, true));
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            EventBus<ObstacleCollidedEvent>.Raise(new ObstacleCollidedEvent(_obstacleType, false));
        }
    }
}
