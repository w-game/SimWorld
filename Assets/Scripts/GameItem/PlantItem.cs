namespace GameItem
{
    public class PlantItem<T> : GameItemBase<T> where T : ConfigBase
    {
        public override string ItemName => "PlantItem";
        public int GrowthTime { get; set; } // 成长时间
        public int GrowthStage { get; set; } // 成长阶段
        public int MaxGrowthStage { get; set; } // 最大成长阶段
        public int GrowthRate { get; set; } // 成长速度

        void Start()
        {
            // 初始化植物的属性
            GrowthTime = 10;
            GrowthStage = 0;
            MaxGrowthStage = 5;
            GrowthRate = 1;

            // 设置植物的初始状态
            // SetState(GameItemState.Planted);
        }
    }
}