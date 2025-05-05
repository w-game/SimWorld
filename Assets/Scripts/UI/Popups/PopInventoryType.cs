using UI.Elements;
using UI.Models;
using UnityEngine;

namespace UI.Popups
{
    public abstract class PopInventoryType<T> : ViewBase<T> where T : class, IModel
    {
        [SerializeField] protected InventoryContainerElement containerItemPanel;
        protected abstract Inventory Inventory { get; }
        protected abstract PropType PropType { get; }

        public override void OnShow()
        {
            containerItemPanel.Init(Inventory, PropType, (propItem, slotElement) => OnItemClicked(propItem as PropItem, slotElement));
        }

        protected abstract void OnItemClicked(PropItem propItem, ItemSlotElement slotElement);

        public override void OnHide()
        {
            base.OnHide();
            containerItemPanel.OnHide();
        }
    }
}