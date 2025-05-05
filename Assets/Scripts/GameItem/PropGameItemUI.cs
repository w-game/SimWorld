namespace GameItem
{
    public class PropGameItemUI : GameItemUI
    {
        public bool CanBeTake { get; private set; } = false;
        public PropGameItem Item => GameItem as PropGameItem;
        public void OnMouseDown()
        {
            if ((GameItem.Pos - GameManager.I.CurrentAgent.Pos).sqrMagnitude < 2)
            {
                Item.BePickedUp(GameManager.I.CurrentAgent);
            }
            else
            {
                CanBeTake = true;
            }
        }
        
        public override void OnGet()
        {
            base.OnGet();
            CanBeTake = false;
        }
    }
}