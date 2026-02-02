using UnityEngine;

namespace OrbitalEscape.UI
{
    public abstract class BaseView : MonoBehaviour, IView
    {
        [SerializeField] private Canvas _root;
        private CanvasGroup _canvasGroup;

        protected CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup != null) 
                    return _canvasGroup;
                
                _canvasGroup ??= _root.GetComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }

        public Canvas Root => _root;
        
        public virtual void Show()
        {
            _root.enabled = true;
            EnableInteraction(true);
        }
        
        public virtual void Hide()
        {
            _root.enabled = false;
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
