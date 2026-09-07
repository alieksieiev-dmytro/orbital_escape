using System;
using OrbitalEscape.Extensions;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.UI
{
    public abstract class BaseController<TView> : IController where TView : IView
    {
        private TView _view;
        [Inject] private IUIManager _uiManager;

        protected TView View
        {
            get
            {
                if (IsNullView) 
                    _view = CreateView();
                return _view;
            }
            set => _view = value;
        }
        
        protected bool IsNullView
        {
            get
            {
                if (_view == null || _view is UnityEngine.Object viewObject && viewObject == null)
                    return true;

                Canvas root = _view.Root;
                return root == null || root.gameObject.IsNull();
            }
        }
        
        public abstract IConvertible Type { get; }

        public abstract void Initialize(ScreenData screenData);
        public abstract void Complete();
        public virtual void RequestClose() => _uiManager.CloseWindow(Type);
        public void SetInteractionSuspended(bool suspended)
        {
            if (!IsNullView)
                View.SetInteractionSuspended(suspended);
        }
        protected abstract TView CreateView();
    }
}
