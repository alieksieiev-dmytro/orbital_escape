using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitalEscape.UI
{
    public class ViewFactory : IViewFactory
    {
        private const string WINDOW = "Window";

        private readonly List<IView> _loadedViews;
        private readonly Transform _root;
        private readonly string _prefabPath;
        
        public ViewFactory(Transform root, string prefabPath)
        {
            _root = root;
            _prefabPath = prefabPath;
            _loadedViews = new List<IView>();
        }

        public event Action<IView> ViewCreated;
        
        public TView Create<TView>(string key) where TView : class, IView => Create<TView>(key, _root);
        
        public TView Create<TView>(string key, Transform root) where TView : class, IView
        {
            if (_loadedViews.Find(view => view.GetType() == typeof(TView)) is TView existingView)
            {
                ViewCreated?.Invoke(existingView);
                return existingView;
            }
            
            string prefabPath = _prefabPath + key + WINDOW;
            var prefab = Resources.Load<GameObject>(prefabPath);
            
            if (prefab == null)
                throw new System.Exception($"Prefab not found at path: {prefabPath}");
            
            var instance = UnityEngine.Object.Instantiate(prefab, root);
            
            instance.transform.localScale = Vector3.one;
            instance.transform.localRotation = Quaternion.identity;
            
            var view = instance.GetComponent<TView>();
            
            if (view == null)
                throw new System.NullReferenceException($"View '{key}' with type '{typeof(TView)}' not found.");
            
            _loadedViews.Add(view);
            ViewCreated?.Invoke(view);
            return view;
        }
        
        public void Dispose()
        {
            foreach (var view in _loadedViews)
            {
                view.Root.gameObject.SetActive(false);
            }
            
            _loadedViews.Clear();
        }
    }
}
