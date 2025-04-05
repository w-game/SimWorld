namespace GameItem
{
    public class FoodItem : GameItemBase
    {
        public override string ItemName => "食物";
        public float FoodValue { get; set; } = 20;
        public int FoodTimes { get; set; } = 5;
    }
}