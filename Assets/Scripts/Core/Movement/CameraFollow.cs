using UnityEngine;

namespace OrbitalEscape.Movement
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Transform _center;
        [SerializeField] private Vector3 _offset;

        private void LateUpdate()
        {
            Vector3 targetPos = _target.position;
            Vector3 centerPos = _center.position;
            
            Vector2 radius = targetPos - centerPos;
            Vector2 tangent = new Vector2(radius.y, -radius.x);           
            
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);

            Vector3 rotatedOffset = rot * _offset;

            Vector3 cameraPos = targetPos + rotatedOffset;
            transform.position = new Vector3(cameraPos.x, cameraPos.y, transform.position.z);
            transform.rotation = rot;
        }
    }
}
