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
        ViewBase View { get; set; }
        void HideUI();
        void ShowUI();
        void SetView(ViewBase view);
    }

    public abstract class ModelBase<T> : IModel where T : ViewBase
    {
        public abstract string Path { get; }
        public abstract ViewType ViewType { get; }
        public object[] Data { get; set; }
        public ViewBase View { get; set; }

        public void ShowUI()
        {
            if (View != null)
            {
                View.gameObject.SetActive(true);
                return;
            }
            if (ViewType == ViewType.View)
            {
            }
            else if (ViewType == ViewType.Popup)
            {
                PopStack.Instance.Push<T>(this, $"Prefabs/UI/Popups/{Path}");
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
            }
            else if (ViewType == ViewType.Element)
            {
            }
        }

        public void SetView(ViewBase view)
        {
            View = view;
            view.Model = this;
            view.OnShow();
        }
    }
}