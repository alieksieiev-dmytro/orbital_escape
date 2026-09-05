using System;
using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Events;
using OrbitalEscape.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace OrbitalEscape.UI
{
    public class UIManager : IUIManager, IInitializable, ITickable, IDisposable
    {
        private readonly DiContainer _diContainer;
        private List<IController> _windowControllers;
        private readonly IInputGate _inputGate;
        private readonly EventBinding<EscapeRequestedEvent> _escapeBinding;
        private IController _activeWindow;
        private bool _ownsInputLock;
        private bool _disposed;
        private EventSystem _suppressedEventSystem;
        private bool _previousNavigationEnabled;
        private int _navigationSuppressedFrame = -1;

        public bool HasOpenWindow => _activeWindow != null;
        
        [Inject]
        public UIManager(DiContainer diContainer, IInputGate inputGate)
        {
            _diContainer = diContainer;
            _inputGate = inputGate;
            _escapeBinding = new EventBinding<EscapeRequestedEvent>(OnEscapeRequested);
        }

        public void Initialize()
        {
            EventBus<EscapeRequestedEvent>.Register(_escapeBinding);
        }

        public void Tick()
        {
            if (_navigationSuppressedFrame >= 0 && Time.frameCount != _navigationSuppressedFrame)
                RestoreNavigation();
        }

        public void OpenWindow(IConvertible windowKey, ScreenData screenData)
        {
            if (_disposed)
                return;

            var controller = GetWindow(windowKey);
            
            if (controller == null)
            {
                throw new InvalidOperationException($"Window controller '{windowKey}' is not bound.");
            }
            
            if (ReferenceEquals(_activeWindow, controller))
                return;

            CloseActiveWindow();
            SuppressNavigationForCurrentFrame();
            _inputGate.SuppressForCurrentFrame();
            _inputGate.Lock();
            _ownsInputLock = true;
            _activeWindow = controller;
            try
            {
                controller.Initialize(screenData);
            }
            catch
            {
                if (ReferenceEquals(_activeWindow, controller))
                    CloseActiveWindow();
                throw;
            }
        }
        
        public void CloseWindow(IConvertible windowKey)
        {
            if (_activeWindow != null && _activeWindow.Type.Equals(windowKey))
                CloseActiveWindow();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            EventBus<EscapeRequestedEvent>.Deregister(_escapeBinding);
            try
            {
                CloseActiveWindow();
            }
            finally
            {
                RestoreNavigation();
            }
        }

        private void OnEscapeRequested(EscapeRequestedEvent unused)
        {
            if (_disposed)
                return;

            SuppressNavigationForCurrentFrame();
            if (_activeWindow != null)
            {
                _activeWindow.RequestClose();
                return;
            }

            if (!_inputGate.IsLocked)
                EventBus<CommandCalledEvent>.Raise(new CommandCalledEvent(nameof(CommandType.Pause)));
        }

        private void CloseActiveWindow()
        {
            if (_activeWindow == null)
                return;

            IController controller = _activeWindow;
            _activeWindow = null;
            SuppressNavigationForCurrentFrame();
            _inputGate.SuppressForCurrentFrame();
            try
            {
                controller.Complete();
            }
            finally
            {
                if (_ownsInputLock)
                {
                    _ownsInputLock = false;
                    _inputGate.Unlock();
                }
            }
        }

        private IController GetWindow(IConvertible windowKey)
        {
            _windowControllers ??= _diContainer.ResolveAll<IController>().ToList();
            return _windowControllers.FirstOrDefault(controller => controller.Type.Equals(windowKey));
        }

        private void SuppressNavigationForCurrentFrame()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;

            if (_suppressedEventSystem != eventSystem)
            {
                RestoreNavigation();
                _suppressedEventSystem = eventSystem;
                _previousNavigationEnabled = eventSystem.sendNavigationEvents;
            }

            _navigationSuppressedFrame = Time.frameCount;
            eventSystem.sendNavigationEvents = false;
        }

        private void RestoreNavigation()
        {
            if (_suppressedEventSystem != null)
                _suppressedEventSystem.sendNavigationEvents = _previousNavigationEnabled;

            _suppressedEventSystem = null;
            _navigationSuppressedFrame = -1;
        }
    }
}
