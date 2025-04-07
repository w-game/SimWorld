using System;

namespace GameItem
{
    public class FoodItem : GameItemBase
    {
        public override string ItemName => "食物";
        public float FoodValue { get; set; } = 20;
        public int MaxFoodTimes { get; set; } = 5;
        public int FoodTimes { get; set; } = 5;


        public void Init(PropItem propItem)
        {
            PropItem = propItem;
        }

        internal void DecreaseFoodTimes()
        {
            FoodTimes--;
            if (FoodTimes <= 0)
            {
                MapManager.I.RemoveGameItem(this);
                Destroy(gameObject);
            }
        }
    }
}