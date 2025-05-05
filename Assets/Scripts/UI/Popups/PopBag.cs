using UI.Elements;
using UI.Models;
using UnityEngine;

namespace UI.Popups
{
    public class PopBag : PopInventoryType<PopBagModel>
    {
        [SerializeField] private InventoryContainerElement chestContainerPanel;
        protected override Inventory Inventory =>
            GameManager.I.CurrentAgent.Bag;

        protected override PropType PropType => PropType.None;

        public override void OnShow()
        {
            base.OnShow();

            if (Model.containerItem != null)
            {
                chestContainerPanel.gameObject.SetActive(true);
                chestContainerPanel.Init(Model.containerItem.Inventory, PropType, propItem =>
                {
                    OnItemClicked(propItem as PropItem);
                });
            }
            else
            {
                chestContainerPanel.gameObject.SetActive(false);
            }
        }

        protected override void OnItemClicked(PropItem propItem)
        {
            // if (config is PropConfig propConfig)
            // {

            // }
        }

        public override void OnHide()
        {
            base.OnHide();
            if (Model.containerItem != null)
            {
                chestContainerPanel.OnHide();
            }
        }
    }
}