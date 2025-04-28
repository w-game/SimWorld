using System;
using System.Collections.Generic;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups
{
    public class PopCraft : ViewBase
    {
        [SerializeField] private ItemSlotElement craftItemPrefab;
        [SerializeField] private Transform craftItemParent;

        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Transform materialsParent;

        [SerializeField] private MaterialElement materialItemPrefab;
        [SerializeField] private Button craftButton;
        private List<MaterialElement> materialItemSlots = new List<MaterialElement>();

        private List<ItemSlotElement> craftItemSlots = new List<ItemSlotElement>();

        private CraftPropItem _selectedItem;
        public override void OnShow()
        {
            base.OnShow();

            var configs = ConfigReader.GetAllConfigs<CraftConfig>();

            foreach (var config in configs)
            {
                ItemSlotElement slot = Instantiate(craftItemPrefab, craftItemParent);
                craftItemSlots.Add(slot);
                config.icon = ConfigReader.GetConfig<PropConfig>(config.id).icon;
                slot.Init(new CraftPropItem(config, 1), OnItemClicked);
            }

            OnItemClicked(craftItemSlots[0].PropItem);

            craftButton.onClick.AddListener(OnCraftButtonClicked);
            GameManager.I.CurrentAgent.Bag.OnInventoryChanged += UpdateCraftButton;
        }

        private void UpdateCraftButton()
        {
            if (_selectedItem == null)
                return;

            OnItemClicked(_selectedItem);
        }

        private void OnItemClicked(PropItemBase propItem)
        {
            _selectedItem = propItem as CraftPropItem;

            foreach (var slot in materialItemSlots)
            {
                Destroy(slot.gameObject);
            }
            materialItemSlots.Clear();

            var propConfig = ConfigReader.GetConfig<PropConfig>(_selectedItem.Config.id);
            icon.sprite = Resources.Load<Sprite>(propConfig.icon);
            nameText.text = propConfig.name;
            // descriptionText.text = propConfig.description;

            var canCraft = true;
            foreach (var material in _selectedItem.Config.materials)
            {
                MaterialElement materialSlot = Instantiate(materialItemPrefab, materialsParent);
                materialItemSlots.Add(materialSlot);
                materialSlot.Init(material);

                var materialConfig = ConfigReader.GetConfig<PropConfig>(material.id);
                if (GameManager.I.CurrentAgent.Bag.GetItem(materialConfig) < material.amount)
                {
                    canCraft = false;
                }
            }

            craftButton.interactable = canCraft;
        }

        private void OnCraftButtonClicked()
        {
            if (_selectedItem != null)
            {
                GameManager.I.CraftItem(_selectedItem.Config as CraftConfig);
            }
        }

        public override void OnHide()
        {
            base.OnHide();
            GameManager.I.CurrentAgent.Bag.OnInventoryChanged -= UpdateCraftButton;
        }
    }
}