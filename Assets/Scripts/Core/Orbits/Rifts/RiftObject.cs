using UnityEngine;

namespace OrbitalEscape
{
    public class RiftObject : MonoBehaviour
    {
        [SerializeField] private GameObject _colliderObject;
        
        public void SetScale(Vector3 arcObjectScale, Vector3 colliderScale)
        {
            transform.localScale = arcObjectScale;
            _colliderObject.transform.localScale = colliderScale;
        }

        public void SetColliderActive(bool isActive)
        {
            if (_colliderObject != null)
            {
                _colliderObject.SetActive(isActive);
            }
        }
    }
}
