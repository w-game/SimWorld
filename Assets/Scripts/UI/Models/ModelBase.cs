using System.Collections.Generic;

namespace UI.Models
{
    public interface IModel
    {
        private static List<IModel> Models { get; } = new List<IModel>();
        public static IModel GetModel<Y>() where Y : IModel, new()
        {
            foreach (var model in Models)
            {
                if (model is Y yModel)
                {
                    return yModel;
                }
            }

            var newModel = new Y();
            Models.Add(newModel);
            return newModel;
        }
        
        string Path { get; }
        ViewType ViewType { get; }
        object[] Data { get; }
        IView View { get; set; }
        void HideUI();
        void ShowUI(params object[] data);
        void SetView(IView view);
    }

    public abstract class ModelBase<T> : IModel where T : IView
    {
        public abstract string Path { get; }
        public abstract ViewType ViewType { get; }
        public object[] Data { get; private set; }
        public IView View { get; set; }

        public void ShowUI(params object[] data)
        {
            if (View != null)
            {
                View.self.SetActive(true);
                return;
            }

            Data = data;
            if (ViewType == ViewType.View)
            {
            }
            else if (ViewType == ViewType.Popup)
            {
                var view = PopStack.Instance.Push<T>(this, $"Prefabs/UI/Popups/{Path}");
                SetView(view);
                OnShow();
                view.OnShow();
            }
            else if (ViewType == ViewType.Element)
            {
            }
        }

        protected virtual void OnShow()
        {
            
        }

        public void HideUI()
        {
            if (ViewType == ViewType.View)
            {
            }
            else if (ViewType == ViewType.Popup)
            {
                View.OnHide();
                PopStack.Instance.Pop(this);
                View = null;

                OnHideUI();
            }
            else if (ViewType == ViewType.Element)
            {
            }
        }

        protected virtual void OnHideUI() { }

        public void SetView(IView view)
        {
            View = view;
            view.SetModel(this);
        }
    }
}