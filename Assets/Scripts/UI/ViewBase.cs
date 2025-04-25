using System.Collections.Generic;
using System.IO;
using UI.Models;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public enum ViewType
    {
        View,
        Popup,
        Element
    }
    public class ViewBase : MonoBehaviour
    {
        [SerializeField] private Button closeBtn;

        public IModel Model { get; set; }
        void Awake()
        {
            closeBtn?.onClick.AddListener(Close);
        }

        public virtual void OnShow()
        {
        }

        public virtual void OnHide()
        {
            closeBtn.onClick.RemoveListener(Close);
        }

        private void Close()
        {
            Model.HideUI();
        }
    }

    // UIStack manages a stack of views for navigation
    public class UIStack : MonoBehaviour
    {
        protected readonly List<IModel> ViewStack = new();

        public virtual void Push<T>(IModel model, string path) where T : ViewBase
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab at path: {path}");
                return;
            }

            T view = Instantiate(prefab, transform).GetComponent<T>();
            if (view == null)
            {
                Debug.LogError($"The prefab loaded from {path} does not contain a ViewBase component.");
                return;
            }
            
            ViewStack.Add(model);
            model.SetView(view);
        }

        // Pop the current view from the stack
        public virtual void Pop(IModel model)
        {
            if (ViewStack.Count > 0)
            {
                if (ViewStack.Contains(model))
                {
                    ViewStack.Remove(model);
                    Destroy(model.View.gameObject);
                }
            }
        }

        // Clear all views from the stack
        public void Clear()
        {
            while (ViewStack.Count > 0)
            {
                var model = ViewStack[^1];
                ViewStack.RemoveAt(ViewStack.Count - 1);
                model.HideUI();
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
        }

        public override void Push<T>(IModel model, string path)
        {
            if (ViewStack.Count > 0)
            {
                var previousModel = ViewStack[^1];
                previousModel.View.gameObject.SetActive(false);
                previousModel.View.OnHide();
            }
            base.Push<T>(model, path);
        }

        public override void Pop(IModel model)
        {
            base.Pop(model);
            if (ViewStack.Count > 0)
            {
                var previousModel = ViewStack[^1];
                previousModel.ShowUI();
            }
        }
    }
}