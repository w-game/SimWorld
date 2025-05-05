using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Elements
{
    public class InventoryContainerElement : MonoBehaviour
    {
        private List<ItemSlotElement> _inventorySlots = new List<ItemSlotElement>();

        public Inventory Inventory { get; private set; }
        private PropType _filterType;

        public void Init(Inventory inventory, PropType propType, UnityAction<PropItemBase> onItemClicked)
        {
            Inventory = inventory;
            _filterType = propType;
            for (int i = 0; i < Inventory.MaxSize; i++)
            {
                ItemSlotElement slot = UIManager.I.UIPool.Get<ItemSlotElement>("Prefabs/UI/Elements/ItemSlotElement", transform.position, transform);
                _inventorySlots.Add(slot);

                slot.Init(null, onItemClicked);
            }

            Inventory.OnInventoryChanged += UpdateBag;
            UpdateBag(null, 0);
        }

        private void UpdateBag(PropItem propItem, int quantity)
        {
            int inventoryIdx = 0;
            for (int i = 0; i < _inventorySlots.Count; i++)
            {
                _inventorySlots[i].Clear();
            }

            for (int i = 0; i < Inventory.Items.Count; i++)
            {
                var item = Inventory.Items[i];
                if (_filterType == PropType.None)
                {
                    _inventorySlots[i].UpdateItemSlot(item, item.Quantity);
                }
                else if (item.Type == _filterType)
                {
                    _inventorySlots[inventoryIdx].UpdateItemSlot(item, item.Quantity);
                    inventoryIdx++;
                }
            }
        }

        internal void OnHide()
        {
            Inventory.OnInventoryChanged -= UpdateBag;
        }
    }
}