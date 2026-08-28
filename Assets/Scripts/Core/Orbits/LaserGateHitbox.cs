using OrbitalEscape.Events;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class LaserGateHitbox : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D _collider;

        private bool _dangerActive;
        private bool _playerInside;
        private bool _raisedDuringActive;

        private void Reset()
        {
            ResolveReferences();
            ConfigureCollider();
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigureCollider();
        }

        public void SetDangerActive(bool dangerActive)
        {
            _dangerActive = dangerActive;
            _raisedDuringActive = false;

            if (_dangerActive)
            {
                TryRaiseLethalCollision();
            }
        }

        public void Fit(float width, float height, float centerYOffset)
        {
            ResolveReferences();
            if (_collider == null)
            {
                return;
            }

            transform.localPosition = new Vector3(0.0f, centerYOffset, 0.0f);
            transform.localScale = Vector3.one;
            _collider.offset = Vector2.zero;
            _collider.size = new Vector2(
                Mathf.Max(0.01f, width),
                Mathf.Max(0.01f, height));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            _playerInside = true;
            TryRaiseLethalCollision();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            _playerInside = false;
            _raisedDuringActive = false;
        }

        private void TryRaiseLethalCollision()
        {
            if (!_dangerActive || !_playerInside || _raisedDuringActive)
            {
                return;
            }

            _raisedDuringActive = true;
            EventBus<ObstacleCollidedEvent>.Raise(new ObstacleCollidedEvent(ArcObjectType.LaserGate, true));
        }

        private void ResolveReferences()
        {
            if (_collider == null)
            {
                _collider = GetComponent<BoxCollider2D>();
            }
        }

        private void ConfigureCollider()
        {
            if (_collider == null)
            {
                return;
            }

            _collider.isTrigger = true;
            _collider.offset = Vector2.zero;
            _collider.size = Vector2.one;
        }
    }
}
