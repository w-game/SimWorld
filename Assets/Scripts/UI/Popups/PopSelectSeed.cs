using UI.Models;
using UnityEngine;

namespace UI.Popups
{
    public class PopSelectSeed : PopInventoryType<PopSelectSeedModel>
    {
        protected override Inventory Inventory =>
            GameManager.I.CurrentAgent.Bag;

        protected override PropType PropType => Model.SelectItem.PropType;

        protected override void OnItemClicked(PropItem propItem)
        {
            if (propItem == null)
                return;

            Model.OnSelected(propItem.Config.id);
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