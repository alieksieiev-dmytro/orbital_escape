using UnityEngine;

namespace OrbitalEscape.UI
{
    public abstract class BaseView : MonoBehaviour, IView
    {
        [SerializeField] private Canvas _root;
        private CanvasGroup _canvasGroup;

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
        }
        
        public virtual void Hide()
        {
            if (Root == null)
                return;

            Root.enabled = false;
            EnableInteraction(false);
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
