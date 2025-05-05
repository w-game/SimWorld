using UI.Elements;
using UI.Models;
using UnityEngine;

namespace UI.Popups
{
    public abstract class PopInventoryType<T> : ViewBase<T> where T : class, IModel
    {
        [SerializeField] private InventoryContainerElement containerItemPanel;
        protected abstract Inventory Inventory { get; }
        protected abstract PropType PropType { get; }

        public override void OnShow()
        {
            containerItemPanel.Init(Inventory, PropType, propItem => OnItemClicked(propItem as PropItem));
        }

        protected abstract void OnItemClicked(PropItem propItem);

        public override void OnHide()
        {
            base.OnHide();
            containerItemPanel.OnHide();
        }
    }
}