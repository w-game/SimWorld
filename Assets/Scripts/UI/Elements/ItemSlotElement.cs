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
        private Transform _slotParent;
        private SlotInfoPanel _slotInfoPanel;

        public PropItemBase PropItem { get; private set; }
        public void Init(PropItemBase propItem, UnityAction<PropItemBase> onClick, Transform slotParent)
        {
            _slotParent = slotParent;
            UpdateItemSlot(propItem, -1);
            itemButton.onClick.AddListener(() => onClick(PropItem));
        }

        public void OnGet()
        {
            
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_slotParent == null || PropItem == null || _slotInfoPanel != null)
                return;
            Vector3 pos = transform.position;
            pos += new Vector3(50, -50, 0);
            _slotInfoPanel = UIManager.I.GetElement<SlotInfoPanel>("Prefabs/UI/Elements/SlotInfoPanel", pos, _slotParent);
            _slotInfoPanel.UpdateInfo(PropItem.Config as PropConfig);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_slotParent == null || PropItem == null || _slotInfoPanel == null)
                return;
            UIManager.I.ReleaseElement(_slotInfoPanel, "Prefabs/UI/Elements/SlotInfoPanel");
            _slotInfoPanel = null;
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

            if (_slotInfoPanel != null)
            {
                _slotInfoPanel.UpdateInfo(propItem.Config as PropConfig);
            }
        }

        internal void Clear()
        {
            PropItem = null;
            itemIcon.gameObject.SetActive(false);
            itemCountText.gameObject.SetActive(false);
            itemButton.interactable = false;

            if (_slotInfoPanel != null)
            {
                UIManager.I.ReleaseElement(_slotInfoPanel, "Prefabs/UI/Elements/SlotInfoPanel");
                _slotInfoPanel = null;
            }
        }
    }
}