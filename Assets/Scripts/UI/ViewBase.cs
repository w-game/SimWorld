using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    // Base class for all UI views
    public class ViewBase : MonoBehaviour
    {
        // Called when the view is displayed
        public virtual void Show()
        {
            // Override to implement view appearance logic
        }

        // Called when the view is hidden
        public virtual void Hide()
        {
            // Override to implement view disappearance logic
        }
    }

    // UIStack manages a stack of views for navigation
    public class UIStack : MonoBehaviour
    {
        private readonly Stack<ViewBase> _viewStack = new();

        public ViewBase CurrentView => _viewStack.Count > 0 ? _viewStack.Peek() : null;

        public void Push(string prefabPath)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab at path: {prefabPath}");
                return;
            }

            ViewBase view = Instantiate(prefab).GetComponent<ViewBase>();
            if (view == null)
            {
                Debug.LogError($"The prefab loaded from {prefabPath} does not contain a ViewBase component.");
                return;
            }

            if (_viewStack.Count > 0)
            {
                _viewStack.Peek().Hide();
            }
            _viewStack.Push(view);
            view.Show();
        }

        // Pop the current view from the stack
        public void Pop()
        {
            if (_viewStack.Count > 0)
            {
                var topView = _viewStack.Pop();
                topView.Hide();
                if (_viewStack.Count > 0)
                {
                    // Show the previous view
                    _viewStack.Peek().Show();
                }
            }
        }

        // Clear all views from the stack
        public void Clear()
        {
            while (_viewStack.Count > 0)
            {
                var view = _viewStack.Pop();
                view.Hide();
            }
        }
    }

    /// <summary>
    /// ViewStack is a specialized UIStack that can be extended for additional behavior related to view management.
    /// </summary>
    public class ViewStack : UIStack
    {
        public static ViewStack Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Optionally uncomment the following line if you want the ViewStack to persist between scenes
            // DontDestroyOnLoad(gameObject);
        }
        // Additional functionality can be added here if needed.
    }

    /// <summary>
    /// PopStack is a specialized UIStack intended for scenarios where a different popping behavior may be implemented in the future.
    /// </summary>
    public class PopStack : UIStack
    {
        public static PopStack Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Optionally uncomment the following line if you want the PopStack to persist between scenes
            // DontDestroyOnLoad(gameObject);
        }
        // Custom pop-related functionality can be added here in the future.
    }
}