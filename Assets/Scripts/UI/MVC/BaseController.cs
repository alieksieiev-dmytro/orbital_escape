using System;
using OrbitalEscape.Extensions;

namespace OrbitalEscape.UI
{
    public abstract class BaseController<TView> : IController where TView : IView
    {
        private TView _view;

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
        
        protected bool IsNullView => _view == null || _view.Root == null || _view.Root.gameObject.IsNull();
        
        public abstract IConvertible Type { get; }
        
        public event Action<IConvertible, ScreenData> ScreenRequested;

        public abstract void Initialize(ScreenData screenData);
        protected abstract TView CreateView();
    }
}
