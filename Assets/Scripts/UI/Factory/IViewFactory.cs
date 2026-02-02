using System;

namespace OrbitalEscape.UI
{
    public interface IViewFactory : IDisposable
    {
        TView Create<TView>(string key) where TView : class, IView;
        TView Create<TView>(string key, UnityEngine.Transform root) where TView : class, IView;
    }
}
