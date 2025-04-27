using System.Collections.Generic;
using UI.Elements;
using UnityEngine;

namespace UI.Popups
{
    public abstract class PopInventoryType : ViewBase
    {
        [SerializeField] private ItemSlotElement itemSlotPrefab;
        [SerializeField] private Transform itemSlotParent;
        private List<ItemSlotElement> inventorySlots = new List<ItemSlotElement>();

        protected abstract int SlotAmount { get; }
        protected abstract Inventory Inventory { get; }
        protected abstract PropType PropType { get; }
        public override void OnShow()
        {
            base.OnShow();

            for (int i = 0; i < SlotAmount; i++)
            {
                ItemSlotElement slot = Instantiate(itemSlotPrefab, itemSlotParent);
                inventorySlots.Add(slot);

                slot.Init(null, propItem => OnItemClicked(propItem as PropItem));
            }

            UpdateBag();
            Inventory.OnInventoryChanged += UpdateBag;
        }

        private void UpdateBag()
        {
            int inventoryIdx = 0;
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                inventorySlots[i].Clear();
            }
            
            for (int i = 0; i < Inventory.Items.Count; i++)
            {
                var item = Inventory.Items[i];
                if (PropType == PropType.None)
                {
                    inventorySlots[i].UpdateItemSlot(item, item.Quantity);
                }
                else if (item.Type == PropType)
                {
                    inventorySlots[inventoryIdx].UpdateItemSlot(item, item.Quantity);
                    inventoryIdx++;
                }
            }
        }

        protected abstract void OnItemClicked(PropItem propItem);

        public override void OnHide()
        {
            base.OnHide();
            GameManager.I.CurrentAgent.Bag.OnInventoryChanged -= UpdateBag;
        }
    }
}