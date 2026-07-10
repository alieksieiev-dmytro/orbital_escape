using OrbitalEscape.Events;
using UnityEngine;

namespace OrbitalEscape.Visuals
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class JumpAnimationController : MonoBehaviour
    {
        private ParticleSystem _particles;
        private EventBinding<PlayerJumpedEvent> _playerJumpedBinding;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            _playerJumpedBinding = new EventBinding<PlayerJumpedEvent>(OnPlayerJumped);
        }

        private void OnEnable()
        {
            EventBus<PlayerJumpedEvent>.Register(_playerJumpedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlayerJumpedEvent>.Deregister(_playerJumpedBinding);
        }

        private void OnPlayerJumped()
        {
            _particles.Emit(1);
        }
    }
}
