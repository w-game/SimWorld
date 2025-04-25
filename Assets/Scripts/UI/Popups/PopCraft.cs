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
        private List<MaterialElement> materialItemSlots = new List<MaterialElement>();

        private List<ItemSlotElement> craftItemSlots = new List<ItemSlotElement>();
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
        }

        private void OnItemClicked(ConfigBase config)
        {
            CraftConfig craftConfig = (CraftConfig)config;

            foreach (var slot in materialItemSlots)
            {
                Destroy(slot.gameObject);
            }
            materialItemSlots.Clear();

            var propConfig = GameManager.I.ConfigReader.GetConfig<PropConfig>(craftConfig.id);
            icon.sprite = Resources.Load<Sprite>(propConfig.icon);
            nameText.text = propConfig.name;
            // descriptionText.text = propConfig.description;
            foreach (var material in craftConfig.materials)
            {
                MaterialElement materialSlot = Instantiate(materialItemPrefab, materialsParent);
                materialItemSlots.Add(materialSlot);
                materialSlot.Init(material);
            }
        }
    }
}