using System;

namespace GameItem
{
    public class FoodItem : PropGameItem
    {
        public float FoodValue { get; set; } = 20;
        public int MaxFoodTimes { get; set; } = 5;
        public int FoodTimes { get; set; } = 5;

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