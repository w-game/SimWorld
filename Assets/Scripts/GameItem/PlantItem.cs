using System.Collections.Generic;
using AI;
using UnityEngine;
using UnityEngine.Events;

namespace GameItem
{
    public enum PlantStage
    {
        Seed,
        Sprout,
        Flowering,
        Harvestable
    }

    public enum PlantState
    {
        None,
        // 干旱
        Drought,
        // 杂草
        Weeds
    }

    public class PlantItem : GameItemBase
    {
        public float GrowthTime { get; set; } // 成长时间
        public PlantStage GrowthStage { get; set; } // 成长阶段
        public int GrowthRate { get; set; } // 成长速度

        public event UnityAction<PlantItem> OnEventInvoked;

        private PlantState _state;
        public PlantState State
        {
            get => _state;
            private set
            {
                _state = value;
                if (_state != PlantState.None)
                {
                    // 处理植物状态变化
                    OnEventInvoked?.Invoke(this);
                }
            }
        }

        public override void ShowUI()
        {
            base.ShowUI();

            var resourceConfig = ConvtertConfig<ResourceConfig>();
            UI.SetRenderer(resourceConfig.stages[(int)GrowthStage]);
        }

        public PlantItem(ConfigBase config, Vector3 pos = default, bool randomStage = false) : base(config, pos)
        {
            if (randomStage)
            {
                GrowthStage = (PlantStage)Random.Range(0, System.Enum.GetValues(typeof(PlantStage)).Length);
            }
            else
            {
                GrowthStage = PlantStage.Seed;
            }
        }

        public override void Update()
        {
            if (GrowthStage == PlantStage.Harvestable)
            {
                return;
            }

            if (State != PlantState.None) return;

            // 一定几率触发事件
            // 例如：生病、干旱、杂草等
            var prob = Random.Range(0, 100);
            
            if (prob < 0.1) // 10% 的几率触发事件
            {
                State = PlantState.Drought;
            }
            else if (prob < 0.2) // 10% 的几率触发事件
            {
                State = PlantState.Weeds;
            }

            GrowthTime += Time.deltaTime * GrowthRate;

            if (GrowthTime >= 100)
            {
                GrowthTime = 0;
                GrowthStage++;
                OnEventInvoked?.Invoke(this);
            }
        }

        public override List<IAction> OnSelected()
        {
            List<IAction> actions = new List<IAction>();
            if (GrowthStage == PlantStage.Harvestable)
            {
                actions.Add(new HarvestAction(this));
            }

            if (State == PlantState.Drought)
            {
                actions.Add(new WaterPlantAction(Pos));
            }
            else if (State == PlantState.Weeds)
            {
                actions.Add(new WeedingAction(this));
            }

            actions.Add(new RemovePlantAction(this));

            return actions;
        }

        internal void Weeding()
        {
            if (State == PlantState.Weeds)
            {
                State = PlantState.None;
            }
        }
    }
}