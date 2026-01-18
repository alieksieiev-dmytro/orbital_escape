using System;
using DG.Tweening;
using OrbitalEscape.Events;
using UnityEngine;

namespace OrbitalEscape.Movement
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Transform _center;
        [SerializeField] private float _radius = 17.5f;
        [SerializeField] private float _innerOffset = 3.35f;
        [SerializeField] private float _outerOffset = 1.65f;

        private EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitEventBinding;

        private void Awake() => 
            _passedThroughOrbitEventBinding = new EventBinding<PassedThroughOrbitEvent>(OnPassedThroughOrbit);

        private void OnEnable() => EventBus<PassedThroughOrbitEvent>.Register(_passedThroughOrbitEventBinding);

        private void OnDisable() => EventBus<PassedThroughOrbitEvent>.Deregister(_passedThroughOrbitEventBinding);

        private void LateUpdate()
        {
            Vector3 centerPos = _center.position;
            Vector3 targetPos = _target.position;

            Vector2 r = targetPos - centerPos;
            float sqrMag = r.sqrMagnitude;

            if (sqrMag < 1e-8f)
            {
                return;
            }

            Vector2 dir = r / Mathf.Sqrt(sqrMag);

            Vector2 camXY = (Vector2)centerPos + dir * _radius;
            transform.position = new Vector3(camXY.x, camXY.y, transform.position.z);

            Vector2 toCenter = (Vector2)centerPos - camXY;
            float angle = Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
        }
        
        private void OnPassedThroughOrbit(PassedThroughOrbitEvent @event)
        {
            float offset = @event.IsOuterOrbit ? _outerOffset : -_innerOffset;
            float targetRadius = @event.Orbit.Radius + offset;
            DOTween.To(
                () => _radius,
                x => _radius = x,
                targetRadius,
                0.5f).SetEase(Ease.InOutSine);
        }
    }
}
