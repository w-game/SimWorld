using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Elements
{
    public class ItemSlotElement : MonoBehaviour, IPoolable
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private Button itemButton;
        [SerializeField] private TextMeshProUGUI itemCountText;

        public PropItemBase PropItem { get; private set; }
        public void Init(PropItemBase propItem, UnityAction<PropItemBase> onClick)
        {
            UpdateItemSlot(propItem, -1);
            itemButton.onClick.AddListener(() => onClick(PropItem));
        }

        public void OnGet()
        {
            
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
        }
    }
}