using UnityEngine;
using UnityEngine.Events;

namespace GameItem
{
    public class PlantItem : GameItemBase
    {
        public int GrowthTime { get; set; } // 成长时间
        public int GrowthStage { get; set; } // 成长阶段
        public int GrowthRate { get; set; } // 成长速度

        public event UnityAction<PlantItem> OnEventInvoked;

        public override void ShowUI()
        {
            base.ShowUI();

            var resourceConfig = ConvtertConfig<ResourceConfig>();
            UI.SetRenderer(resourceConfig.stages[GrowthStage]);
        }

        public PlantItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
            var resourceConfig = ConvtertConfig<ResourceConfig>();
            GrowthStage = Random.Range(0, resourceConfig.stages.Length);
        }
    }
}