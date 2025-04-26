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

        private CraftConfig _selectedConfig;
        public override void OnShow()
        {
            base.OnShow();

            var configs = GameManager.I.ConfigReader.GetAllConfigs<CraftConfig>();

            foreach (var config in configs)
            {
                ItemSlotElement slot = Instantiate(craftItemPrefab, craftItemParent);
                craftItemSlots.Add(slot);
                config.icon = GameManager.I.ConfigReader.GetConfig<PropConfig>(config.id).icon;
                slot.Init(config, OnItemClicked);
            }

            OnItemClicked(configs[0]);

            craftButton.onClick.AddListener(OnCraftButtonClicked);
            GameManager.I.CurrentAgent.Bag.OnInventoryChanged += UpdateCraftButton;
        }

        private void UpdateCraftButton()
        {
            if (_selectedConfig == null)
                return;

            OnItemClicked(_selectedConfig);
        }

        private void OnItemClicked(ConfigBase config)
        {
            _selectedConfig = (CraftConfig)config;

            foreach (var slot in materialItemSlots)
            {
                Destroy(slot.gameObject);
            }
            materialItemSlots.Clear();

            var propConfig = GameManager.I.ConfigReader.GetConfig<PropConfig>(_selectedConfig.id);
            icon.sprite = Resources.Load<Sprite>(propConfig.icon);
            nameText.text = propConfig.name;
            // descriptionText.text = propConfig.description;

            var canCraft = true;
            foreach (var material in _selectedConfig.materials)
            {
                MaterialElement materialSlot = Instantiate(materialItemPrefab, materialsParent);
                materialItemSlots.Add(materialSlot);
                materialSlot.Init(material);

                var materialConfig = GameManager.I.ConfigReader.GetConfig<PropConfig>(material.id);
                if (GameManager.I.CurrentAgent.Bag.GetItem(materialConfig) < material.amount)
                {
                    canCraft = false;
                }
            }

            craftButton.interactable = canCraft;
        }

        private void OnCraftButtonClicked()
        {
            if (_selectedConfig != null)
            {
                GameManager.I.CraftItem(_selectedConfig);
            }
        }

        public override void OnHide()
        {
            base.OnHide();
            GameManager.I.CurrentAgent.Bag.OnInventoryChanged -= UpdateCraftButton;
        }
    }
}