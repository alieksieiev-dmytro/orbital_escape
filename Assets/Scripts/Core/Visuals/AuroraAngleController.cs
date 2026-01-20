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
            _targetRenderer.GetPropertyBlock(_block);
            _block.SetFloat(AuroraAngle, -_cameraTransform.eulerAngles.z);
            _targetRenderer.SetPropertyBlock(_block);
        }
    }
}
