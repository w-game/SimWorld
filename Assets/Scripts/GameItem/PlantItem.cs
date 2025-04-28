using System.Collections.Generic;
using AI;
using Citizens;
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

    public class PlantItem : GameItemBase<ResourceConfig>
    {
        public override bool Walkable => true;
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

            UI.SetRenderer(Config.stages[(int)GrowthStage]);
        }

        public PlantItem(ResourceConfig config, Vector3 pos, bool random) : base(config, pos)
        {
            if (random)
            {
                GrowthStage = (PlantStage)Random.Range(0, config.stages.Length);
            }
            else
            {
                GrowthStage = PlantStage.Seed;
            }
        }

        private void OnGrowth()
        {
            // 处理植物生长
            if (GrowthStage == PlantStage.Harvestable)
            {
                return;
            }

            GrowthTime += GameTime.DeltaTime * GrowthRate;

            if (GrowthTime >= 100)
            {
                GrowthTime = 0;
                GrowthStage++;
                UI.SetRenderer(Config.stages[(int)GrowthStage]);
            }

            if (GrowthStage == PlantStage.Harvestable)
            {
                // 触发成熟事件
                OnEventInvoked?.Invoke(this);
            }
        }

        public List<(PropConfig, int)> CheckDropItems()
        {
            if (GrowthStage != PlantStage.Harvestable)
            {
                return new List<(PropConfig, int)>();
            }
            
            List<(PropConfig, int)> dropItems = new List<(PropConfig, int)>();
            foreach (var dropItem in Config.dropItems)
            {
                var confg = ConfigReader.GetConfig<PropConfig>(dropItem.id);
                dropItems.Add((confg, dropItem.count));
            }

            return dropItems;
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

            OnGrowth();
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            List<IAction> actions = new List<IAction>();

            if (State == PlantState.Drought)
            {
                actions.Add(ActionPool.Get<WaterPlantAction>(Pos));
            }
            else if (State == PlantState.Weeds)
            {
                actions.Add(ActionPool.Get<WeedingAction>(this));
            }

            if (agent is Agent a)
            {
                actions.Add(CheckRemovePlant(a));
            }

            return actions;
        }

        protected virtual IAction CheckRemovePlant(Agent agent) 
        {
            if (GrowthStage == PlantStage.Harvestable)
            {
                return ActionPool.Get<HarvestAction>(this);
            }
            return ActionPool.Get<RemovePlantAction>(this);
        }

        internal void Weeding()
        {
            if (State == PlantState.Weeds)
            {
                State = PlantState.None;
            }
        }
    }
    
    public class TreeItem : PlantItem
    {
        public override bool Walkable => false;
        public TreeItem(ResourceConfig config, Vector3 pos, bool random) : base(config, pos, random)
        {
        }

        protected override IAction CheckRemovePlant(Agent agent)
        {
            // 这里可以添加树木的特殊逻辑
            var axe = agent.Bag.GetItemHasEffect("Chop");

            if (axe != null)
            {
                return ActionPool.Get<RemovePlantAction>(this, "Chop the tree");
                
            }
            else
            {
                // 如果没有斧头，则返回一个默认的 RemovePlantAction
                var removeAction = ActionPool.Get<RemovePlantAction>(this, "Remove Tree (Need Axe)");
                removeAction.Enable = false;
                return removeAction;
            }
        }
    }
}