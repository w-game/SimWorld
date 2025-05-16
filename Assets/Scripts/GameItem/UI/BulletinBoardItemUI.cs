namespace GameItem
{
    public class BulletinBoardItemUI : GameItemUI
    {
        private BulletinBoardItem _item => GameItem as BulletinBoardItem;
        public void OnMouseDown()
        {
            if (UIManager.I.IsClickUI) return;

            _item.OnClick();
        }
    }
}