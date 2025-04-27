namespace UI.Popups
{
    public class PopBag : PopInventoryType
    {
        protected override int SlotAmount => 16;

        protected override Inventory Inventory =>
            GameManager.I.CurrentAgent.Bag;
            
        protected override PropType PropType => PropType.None;

        protected override void OnItemClicked(PropItem propItem)
        {
            // if (config is PropConfig propConfig)
            // {
                
            // }
        }
    }
}