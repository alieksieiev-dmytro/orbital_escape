using OrbitalEscape.Audio;
using OrbitalEscape.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonAudioBehaviour : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        private Button _button;
        private int _lastFocusSoundFrame = -1;

        private bool CanPlay => isActiveAndEnabled && _button != null
            && _button.IsActive() && _button.IsInteractable();

        private void Awake() => _button = GetComponent<Button>();

        public void OnPointerEnter(PointerEventData eventData) => RequestFocusSound();

        public void OnSelect(BaseEventData eventData)
        {
            if (eventData is AutomaticUISelectionEventData || eventData is PointerEventData)
                return;

            RequestFocusSound();
        }

        // Authored once in Button.onClick, before window actions registered at runtime.
        public void PlayClickSound()
        {
            if (CanPlay)
                EventBus<UIButtonSoundRequestedEvent>.Raise(new UIButtonSoundRequestedEvent(SfxType.UIClick));
        }

        private void RequestFocusSound()
        {
            if (!CanPlay || _lastFocusSoundFrame == Time.frameCount)
                return;

            _lastFocusSoundFrame = Time.frameCount;
            EventBus<UIButtonSoundRequestedEvent>.Raise(new UIButtonSoundRequestedEvent(SfxType.UIFocus));
        }
    }
}
