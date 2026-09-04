using Unity.Cinemachine;
using UnityEngine;

namespace OrbitalEscape.Movement
{
    public sealed class CinemachineHorizontalPlayerFraming : CinemachineExtension
    {
        [SerializeField] private Transform _player;

        private float _softZoneWidth;

        public void SetSoftZoneWidth(float width)
        {
            _softZoneWidth = Mathf.Clamp01(width);
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase virtualCamera,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize || _player == null)
            {
                return;
            }

            Quaternion orientation = state.GetFinalOrientation();
            Vector3 cameraRight = orientation * Vector3.right;
            Vector3 playerOffset = _player.position - state.GetFinalPosition();
            float horizontalOffset = Vector3.Dot(playerOffset, cameraRight);
            float horizontalCorrection = GetHorizontalCorrection(horizontalOffset, state.Lens);

            state.PositionCorrection += cameraRight * horizontalCorrection;
        }

        private float GetHorizontalCorrection(float horizontalOffset, LensSettings lens)
        {
            if (_softZoneWidth <= 0.0f || !lens.Orthographic)
            {
                return horizontalOffset;
            }

            float halfZoneWidth = lens.OrthographicSize * lens.Aspect * _softZoneWidth;
            float excessOffset = Mathf.Max(0.0f, Mathf.Abs(horizontalOffset) - halfZoneWidth);
            return Mathf.Sign(horizontalOffset) * excessOffset;
        }
    }
}
