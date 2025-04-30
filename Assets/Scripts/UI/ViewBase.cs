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

    public interface IView
    {
        public GameObject self { get; }
        void OnShow();
        void OnHide();
        void SetModel(IModel model);
    }

    public abstract class ViewBase<T> : MonoBehaviour, IView where T : class, IModel
    {
        [SerializeField] private Button closeBtn;

        public T Model { get; set; }

        public GameObject self => gameObject;

        void Awake()
        {
            closeBtn?.onClick.AddListener(Close);
        }

        public abstract void OnShow();

        public virtual void OnHide()
        {
            closeBtn?.onClick.RemoveListener(Close);
        }

        protected void Close()
        {
            Model.HideUI();
        }

        public void SetModel(IModel model)
        {
            Model = model as T;
            if (Model == null)
            {
                Debug.LogError($"Failed to set model. Expected type: {typeof(T)}, but got: {model.GetType()}");
                return;
            }
            Model.View = this;
        }
    }

    // UIStack manages a stack of views for navigation
    public class UIStack : MonoBehaviour
    {
        protected readonly List<IModel> ViewStack = new();

        public virtual IView Push<T>(IModel model, string path) where T : IView
        {
            var existingModel = ViewStack.Find(x => x.Path == model.Path);
            if (existingModel != null)
            {
                return existingModel.View;
            }
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab at path: {path}");
                return null;
            }

            T view = Instantiate(prefab, transform).GetComponent<T>();
            if (view == null)
            {
                Debug.LogError($"The prefab loaded from {path} does not contain a ViewBase component.");
                return view;
            }

            ViewStack.Add(model);
            return view;
        }

        // Pop the current view from the stack
        public virtual void Pop(IModel model)
        {
            if (ViewStack.Count > 0)
            {
                if (ViewStack.Contains(model))
                {
                    ViewStack.Remove(model);
                    Destroy(model.View.self);
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

        public override IView Push<T>(IModel model, string path)
        {
            if (ViewStack.Count > 0)
            {
                var previousModel = ViewStack[^1];
                previousModel.View.self.SetActive(false);
                previousModel.View.OnHide();
            }
            return base.Push<T>(model, path);
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