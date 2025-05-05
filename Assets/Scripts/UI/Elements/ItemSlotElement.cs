using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Elements
{
    public class ItemSlotElement : MonoBehaviour, IPoolable, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private Button itemButton;
        [SerializeField] private TextMeshProUGUI itemCountText;
        public event Action<ItemSlotElement> OnPointerEnterEvent;
        public event Action<ItemSlotElement> OnPointerExitEvent;

        public PropItemBase PropItem { get; private set; }
        public void Init(PropItemBase propItem, UnityAction<PropItemBase, ItemSlotElement> onClick)
        {
            UpdateItemSlot(propItem, -1);
            itemButton.onClick.AddListener(() => onClick(PropItem, this));
        }

        public void OnGet()
        {

        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnterEvent?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExitEvent?.Invoke(this);
        }

        public void OnRelease()
        {
            Clear();
        }

        public void UpdateItemSlot(PropItemBase propItem, int count = -1)
        {
            if (propItem == null)
            {
                itemIcon.gameObject.SetActive(false);
                itemCountText.gameObject.SetActive(false);
                itemButton.interactable = false;
                return;
            }

            PropItem = propItem;

            itemIcon.sprite = Resources.Load<Sprite>(propItem.Config.icon);
            itemIcon.gameObject.SetActive(true);
            itemButton.interactable = true;
            if (count > 0)
            {
                itemCountText.text = count.ToString();
                itemCountText.gameObject.SetActive(true);
            }
            else
            {
                itemCountText.gameObject.SetActive(false);
            }
        }

        internal void Clear()
        {
            PropItem = null;
            itemIcon.gameObject.SetActive(false);
            itemCountText.gameObject.SetActive(false);
            itemButton.interactable = false;
            OnPointerEnterEvent = null;
            OnPointerExitEvent = null;
            itemButton.onClick.RemoveAllListeners();
        }

        public void OnItemRemoved(int count)
        {
            if (PropItem != null)
            {
                PropItem.AddQuantity(-count);
                if (PropItem.Quantity <= 0)
                {
                    Clear();
                }
                else
                {
                    UpdateItemSlot(PropItem, PropItem.Quantity);
                }
            }
        }
    }
}