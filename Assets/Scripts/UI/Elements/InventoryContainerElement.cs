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

                slot.Init(null, onItemClicked, transform);
            }

            Inventory.OnInventoryChanged += UpdateBag;
            UpdateBag(null, 0);
        }

        private void UpdateBag(PropItem propItem, int quantity)
        {
            ClearSlots();
            for (int i = 0; i < Inventory.MaxSize; i++)
            {
                var slot = UIManager.I.UIPool.Get<ItemSlotElement>("Prefabs/UI/Elements/ItemSlotElement", transform.position, transform);
                _inventorySlots.Add(slot);
            }

            int inventoryIdx = 0;
            for (int i = inventoryIdx; i < Inventory.Items.Count; i++)
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
            ClearSlots();
        }

        private void ClearSlots()
        {
            foreach (var slot in _inventorySlots)
            {
                UIManager.I.UIPool.Release(slot, "Prefabs/UI/Elements/ItemSlotElement");
            }
            _inventorySlots.Clear();
        }
    }
}