using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace OrbitalEscape.Visuals
{
    [CreateAssetMenu(
        fileName = "SpikePresentationConfig",
        menuName = "Scriptable Objects/Configs/Spike Presentation Config")]
    public sealed class SpikePresentationConfig : ScriptableObject
    {
        [field: SerializeField, FormerlySerializedAs("<UpcomingAlphaMultiplier>k__BackingField"), Range(0f, 1f)]
        public float UnreachableAlphaMultiplier { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f)]
        public float TransitionDuration { get; private set; } = 0.15f;

        [field: SerializeField]
        public Ease TransitionEase { get; private set; } = Ease.OutQuad;
    }
}
