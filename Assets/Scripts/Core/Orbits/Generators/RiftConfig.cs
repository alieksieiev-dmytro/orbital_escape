using UnityEngine;

namespace OrbitalEscape
{
    [CreateAssetMenu(fileName = "RiftConfig", menuName = "Scriptable Objects/Configs/Rift Config")]
    public class RiftConfig : ScriptableObject
    {
        [field: SerializeField] public float MinDistanceBetweenRifts { get; private set; }
        [field: SerializeField] public float RiftLength { get; private set; }
        [field: SerializeField] public float RiftCount { get; private set; }
    }
}
