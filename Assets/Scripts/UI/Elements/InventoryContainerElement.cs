using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Elements
{
    public class InventoryContainerElement : MonoBehaviour
    {
        [SerializeField] private Transform _slotsParent;
        private List<ItemSlotElement> _inventorySlots = new List<ItemSlotElement>();

        public Inventory Inventory { get; private set; }
        private PropType _filterType;

        private UnityAction<PropItemBase, ItemSlotElement> onItemClicked;

        public static SlotInfoPanel SlotInfoPanel { get; private set; }
        public static PropItemActionPanel ActionPanel { get; private set; }

        public void Init(Inventory inventory, PropType propType, UnityAction<PropItemBase, ItemSlotElement> onItemClicked)
        {
            Inventory = inventory;
            _filterType = propType;
            this.onItemClicked = onItemClicked;
            Inventory.OnInventoryChanged += UpdateBag;
            UpdateBag(null, 0);
        }

        private void OnSlotClicked(PropItemBase propItem, ItemSlotElement slotElement)
        {
            if (propItem == null)
                return;

            onItemClicked?.Invoke(propItem, slotElement);

            if (ActionPanel == null)
            {
                ActionPanel = UIManager.I.GetElement<PropItemActionPanel>("Prefabs/UI/Elements/PropItemActionPanel", slotElement.transform.position, transform);
            }
            else
            {
                ActionPanel.Clear();
            }

            ReleaseSlotInfoPanel();
            ActionPanel.transform.position = slotElement.transform.position + new Vector3(50, -50, 0);
            ActionPanel.Init(slotElement);
        }

        private void OnSlotPointerEnter(ItemSlotElement slotElement)
        {
            if (slotElement == null || slotElement.PropItem == null || SlotInfoPanel != null || ActionPanel != null)
                return;

            Vector3 pos = slotElement.transform.position;
            pos += new Vector3(50, -50, 0);
            SlotInfoPanel = UIManager.I.GetElement<SlotInfoPanel>("Prefabs/UI/Elements/SlotInfoPanel", pos, transform);
            SlotInfoPanel.UpdateInfo(slotElement.PropItem.Config as PropConfig);
        }

        private void OnSlotPointerExit(ItemSlotElement slotElement)
        {
            if (slotElement == null || SlotInfoPanel == null)
                return;

            UIManager.I.UIPool.Release(SlotInfoPanel, "Prefabs/UI/Elements/SlotInfoPanel");
            SlotInfoPanel = null;
        }

        private void UpdateBag(PropItem propItem, int quantity)
        {
            ClearSlots();
            for (int i = 0; i < Inventory.MaxSize; i++)
            {
                var slot = UIManager.I.UIPool.Get<ItemSlotElement>("Prefabs/UI/Elements/ItemSlotElement", transform.position, _slotsParent);
                _inventorySlots.Add(slot);
                slot.Init(null, OnSlotClicked);
                slot.OnPointerEnterEvent += OnSlotPointerEnter;
                slot.OnPointerExitEvent += OnSlotPointerExit;
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
            ReleaseSlotInfoPanel();
            ReleaseActionPanel();
        }

        private void ClearSlots()
        {
            foreach (var slot in _inventorySlots)
            {
                slot.Clear();
                UIManager.I.UIPool.Release(slot, "Prefabs/UI/Elements/ItemSlotElement");
            }
            _inventorySlots.Clear();
        }

        private void ReleaseSlotInfoPanel()
        {
            if (SlotInfoPanel != null)
            {
                UIManager.I.ReleaseElement(SlotInfoPanel, "Prefabs/UI/Elements/SlotInfoPanel");
                SlotInfoPanel = null;
            }
        }
        private void ReleaseActionPanel()
        {
            if (ActionPanel != null)
            {
                UIManager.I.ReleaseElement(ActionPanel, "Prefabs/UI/Elements/PropItemActionPanel");
                ActionPanel = null;
            }
        }

        void Update()
        {
            if (ActionPanel != null)
            {
                RectTransform rectTransform = ActionPanel.GetComponent<RectTransform>();
                Vector2 localMousePos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, Input.mousePosition, null, out localMousePos);

                // Add 10 pixels padding to the rect
                Rect paddedRect = new Rect(
                    rectTransform.rect.xMin - 50,
                    rectTransform.rect.yMin - 50,
                    rectTransform.rect.width + 100,
                    rectTransform.rect.height + 100
                );

                if (!paddedRect.Contains(localMousePos))
                {
                    ReleaseActionPanel();
                }
            }
        }
    }
}