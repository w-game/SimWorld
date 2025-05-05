using UnityEngine;

namespace GameItem
{
    public class PropGameItemUI : GameItemUI
    {
        public bool CanBeTake { get; set; } = false;
        public PropGameItem Item => GameItem as PropGameItem;
        public CircleCollider2D CircleCollider2D => GetComponent<CircleCollider2D>();
        public void OnMouseDown()
        {
            if ((GameItem.Pos - GameManager.I.CurrentAgent.Pos).sqrMagnitude < CircleCollider2D.radius * CircleCollider2D.radius + 1f)
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