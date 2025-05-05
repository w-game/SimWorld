using AI;
using UI.Elements;
using UI.Models;

namespace UI.Popups
{
    public class PopTrade : PopInventoryType<PopTradeModel>
    {
        public string Title => "Sell Count";

        protected override Inventory Inventory => GameManager.I.CurrentAgent.Bag;

        protected override PropType PropType => PropType.None;

        private PropItem _selectedItem;

        public void Cancel()
        {

        }

        public void Confirm(int count)
        {
            Model.TradeAction.SellItem(_selectedItem, count);
        }

        public override void OnShow()
        {
            base.OnShow();
        }

        public void Update()
        {
            if (GameManager.I.CurrentAgent.Brain.CurAction is not TradeAction)
            {
                Close();
            }
        }

        protected override void OnItemClicked(PropItem propItem, ItemSlotElement slotElement)
        {
            _selectedItem = propItem;
            var countSelectData = new CountSelectData(Title, propItem.Quantity);
            countSelectData.ConfirmEvent += Confirm;
            countSelectData.CancelEvent += Cancel;
            var model = IModel.GetModel<PopCountSelectorModel>();
            model.ShowUI();
        }

        public override void OnHide()
        {
            base.OnHide();
            Model.TradeAction.EndTrade();
        }
    }
}