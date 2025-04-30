using System.Collections.Generic;

namespace UI.Models
{
    public interface IModel
    {
        private static List<IModel> Models { get; } = new List<IModel>();
        public static IModel GetModel<Y>(params object[] data) where Y : IModel, new()
        {
            foreach (var model in Models)
            {
                if (model is Y yModel)
                {
                    yModel.Data = data;
                    return yModel;
                }
            }

            var newModel = new Y();
            newModel.Data = data;
            Models.Add(newModel);
            return newModel;
        }
        
        string Path { get; }
        ViewType ViewType { get; }
        object[] Data { get; set; }
        IView View { get; set; }
        void HideUI();
        void ShowUI();
        void SetView(IView view);
    }

    public abstract class ModelBase<T> : IModel where T : IView
    {
        public abstract string Path { get; }
        public abstract ViewType ViewType { get; }
        public object[] Data { get; set; }
        public IView View { get; set; }

        public void ShowUI()
        {
            if (View != null)
            {
                View.self.SetActive(true);
                return;
            }
            if (ViewType == ViewType.View)
            {
            }
            else if (ViewType == ViewType.Popup)
            {
                var view = PopStack.Instance.Push<T>(this, $"Prefabs/UI/Popups/{Path}");
                SetView(view);
                view.OnShow();
            }
            else if (ViewType == ViewType.Element)
            {
            }
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