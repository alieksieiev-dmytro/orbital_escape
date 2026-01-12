using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector2 _offset;

    private void Awake()
    {
        GetComponent<Camera>().orthographic = true;
    }

    private void LateUpdate()
    {
        if (_target == null) { return; }

        Vector3 targetPos = _target.position + new Vector3(_offset.x, _offset.y, 0f);
        Quaternion targetRot = new Quaternion(0f, 0f, _target.rotation.z, _target.rotation.w);
        transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        transform.eulerAngles = new Vector3(targetRot.eulerAngles.x, targetRot.eulerAngles.y, targetRot.eulerAngles.z - 180f);
    }
}
