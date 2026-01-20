using UnityEngine;

namespace OrbitalEscape.Visuals
{
    public class AuroraAngleController : MonoBehaviour
    {
        private static readonly int AuroraAngle = Shader.PropertyToID("_AuroraAngle");
        
        [SerializeField] private SpriteRenderer _targetRenderer;

        private MaterialPropertyBlock _block;
        private Transform _cameraTransform;

        public void Awake()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<SpriteRenderer>();
            }

            _block = new MaterialPropertyBlock();
            _cameraTransform = Camera.main?.transform;
        }

        public void LateUpdate()
        {
            Vector2 cameraPosition = _cameraTransform.position;
            Vector2 centerPosition = Vector2.zero;

            Vector2 dir = (centerPosition - cameraPosition).normalized;

            float angleRad = Mathf.Atan2(-dir.x, dir.y);
            float angleDeg = angleRad * Mathf.Rad2Deg;

            _targetRenderer.GetPropertyBlock(_block);
            _block.SetFloat(AuroraAngle, -_cameraTransform.eulerAngles.z);
            _targetRenderer.SetPropertyBlock(_block);
        }
    }
}
