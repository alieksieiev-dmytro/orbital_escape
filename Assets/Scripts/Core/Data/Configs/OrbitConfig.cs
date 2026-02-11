using UnityEngine;

namespace OrbitalEscape
{
    [CreateAssetMenu(fileName = "OrbitConfig", menuName = "Scriptable Objects/Configs/Orbit Config")]
    public class OrbitConfig : ScriptableObject
    {
        [field: SerializeField] public float FirstOrbitRadius { get; private set; }
        [field: SerializeField] public float OrbitRadiusStep { get; private set; }
    }
}
