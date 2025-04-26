using System.Collections.Generic;
using UI.Elements;
using UnityEngine;

namespace UI.Popups
{
    public class PopBag : ViewBase
    {
        [SerializeField] private ItemSlotElement itemSlotPrefab;
        [SerializeField] private Transform itemSlotParent;
        private List<ItemSlotElement> inventorySlots = new List<ItemSlotElement>();
        public override void OnShow()
        {
            base.OnShow();

            for (int i = 0; i < 16; i++)
            {
                ItemSlotElement slot = Instantiate(itemSlotPrefab, itemSlotParent);
                inventorySlots.Add(slot);

                slot.Init(null, OnItemClicked);
            }

            UpdateBag();
            GameManager.I.CurrentAgent.Bag.OnInventoryChanged += UpdateBag;
        }

        private void UpdateBag()
        {
            for (int i = 0; i < GameManager.I.CurrentAgent.Bag.Items.Count; i++)
            {
                var item = GameManager.I.CurrentAgent.Bag.Items[i];
                inventorySlots[i].UpdateItemSlot(item.Config, item.Quantity);
            }
        }

        private void OnItemClicked(ConfigBase config)
        {

        }
        
        public override void OnHide()
        {
            base.OnHide();
            GameManager.I.CurrentAgent.Bag.OnInventoryChanged -= UpdateBag;
        }
    }
}