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

        public override void Init(ConfigBase config)
        {
            base.Init(config);

            var resourceConfig = ConvtertConfig<ResourceConfig>();
            GrowthStage = Random.Range(0, resourceConfig.stages.Length);
            _sr.sprite = Resources.Load<Sprite>(resourceConfig.stages[GrowthStage]);
        }

        void Update()
        {
            
        }
    }
}