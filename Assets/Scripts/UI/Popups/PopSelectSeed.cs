using UI.Models;
using UnityEngine;

namespace UI.Popups
{
    public class PopSelectSeed : PopInventoryType<PopSelectSeedModel>
    {
        protected override int SlotAmount => 16;

        protected override Inventory Inventory =>
            GameManager.I.CurrentAgent.Bag;

        private PopSelectSeedModel SelfModel => (PopSelectSeedModel)base.Model;
        protected override PropType PropType => SelfModel.PropType;

        protected override void OnItemClicked(PropItem propItem)
        {
            if (propItem == null)
                return;

            if (Model is PopSelectSeedModel model)
            {
                model.OnSelected(propItem.Config.id);
            }
            Close();
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