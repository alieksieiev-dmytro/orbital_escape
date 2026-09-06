using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    public abstract class BaseView : MonoBehaviour, IView
    {
        [SerializeField] private Canvas _root;
        [SerializeField] private Selectable _initialSelection;
        private CanvasGroup _canvasGroup;
        private Selectable[] _selectables;
        private bool _isShown;

        public Canvas Root => _root != null ? _root : _root = GetComponent<Canvas>();

        protected CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup != null) 
                    return _canvasGroup;

                if (Root == null)
                    return null;

                _canvasGroup ??= Root.GetComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }
        
        public virtual void Show()
        {
            if (Root == null)
                return;

            Root.enabled = true;
            EnableInteraction(true);
            _isShown = true;
            RestoreNavigationSelection();
        }
        
        public virtual void Hide()
        {
            _isShown = false;
            ClearNavigationSelection();
            if (Root == null)
                return;

            Root.enabled = false;
            EnableInteraction(false);
        }

        protected virtual void LateUpdate()
        {
            if (_isShown)
                RestoreNavigationSelection();
        }

        protected void RestoreNavigationSelection()
        {
            EventSystem eventSystem = EventSystem.current;
            if (!_isShown || Root == null || !Root.enabled || eventSystem == null || eventSystem.alreadySelecting)
                return;

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected != null && IsAvailable(selected.GetComponent<Selectable>()))
                return;

            Selectable next = IsAvailable(_initialSelection) ? _initialSelection : null;
            if (next == null)
            {
                _selectables ??= GetComponentsInChildren<Selectable>(true);
                foreach (Selectable candidate in _selectables)
                {
                    if (IsAvailable(candidate))
                    {
                        next = candidate;
                        break;
                    }
                }
            }

            if (selected == null && next == null)
                return;

            eventSystem.SetSelectedGameObject(next != null ? next.gameObject : null,
                new AutomaticUISelectionEventData(eventSystem));
        }

        private bool IsAvailable(Selectable selectable)
        {
            return selectable != null && selectable.transform.IsChildOf(transform)
                && selectable.IsActive() && selectable.IsInteractable()
                && selectable.navigation.mode != Navigation.Mode.None;
        }

        private void ClearNavigationSelection()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || eventSystem.alreadySelecting)
                return;

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform))
                eventSystem.SetSelectedGameObject(null);
        }

        private void EnableInteraction(bool enable)
        {
            if (CanvasGroup == null)
                return;

            CanvasGroup.blocksRaycasts = enable;
            CanvasGroup.interactable = enable;
        }
    }
}
