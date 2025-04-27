using UI.Models;
using UnityEngine;

namespace UI.Popups
{
    public class PopSelectSeed : PopInventoryType
    {
        protected override int SlotAmount => 16;

        protected override Inventory Inventory =>
            GameManager.I.CurrentAgent.Bag;

        protected override PropType PropType => PropType.Seed;

        protected override void OnItemClicked(ConfigBase config)
        {
            if (config is not PropConfig propConfig)
                return;
                
            if (Model is PopSelectSeedModel model)
            {
                model.ExecuteCallback(propConfig.id);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                Close();
            }
        }
    }
}