using UnityEngine;

namespace OrbitalEscape.UI
{
    public interface IView
    {
        Canvas Root { get; }
        
        void Show();
        void Hide();
    }
}
