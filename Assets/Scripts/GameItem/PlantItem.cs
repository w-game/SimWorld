using System.Collections.Generic;
using AI;
using Citizens;
using UnityEngine;
using UnityEngine.Events;

namespace GameItem
{
    public enum PlantStage
    {
        Seed = 0,
        Sprout = 1,
        Flowering = 2,
        Harvestable = 3
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
        public int GrowthRate { get; set; } = 1; // 成长速度
        protected virtual bool Drought { get; set; } = true;

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

        private float _waterTime;

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
            if (GrowthStage == PlantStage.Harvestable || State != PlantState.None)
            {
                return;
            }

            GrowthTime += GameTime.DeltaTime * GrowthRate;

            if (GrowthTime >= 100)
            {
                GrowthTime = 0;
                GrowthStage++;
                if ((int)GrowthStage >= Config.stages.Length - 1)
                {
                    GrowthStage = PlantStage.Harvestable;
                }
                UI.SetRenderer(Config.stages[(int)GrowthStage]);
            }

            if (GrowthStage == PlantStage.Harvestable)
            {
                // 触发成熟事件
                OnEventInvoked?.Invoke(this);
            }
            else
            {
                CheckDrought();
            }
        }

        private void CheckDrought()
        {
            // 处理植物干旱
            if (State == PlantState.Drought || !Drought)
            {
                return;
            }

            _waterTime += GameTime.DeltaTime;
            if (_waterTime >= 24 * 60 * 60)
            {
                _waterTime = 0;
                State = PlantState.Drought;
            }
        }

        public virtual List<(PropConfig, int)> CheckDropItems()
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
            OnGrowth();
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            List<IAction> actions = new List<IAction>();

            if (State == PlantState.Weeds)
            {
                actions.Add(ActionPool.Get<WeedingAction>(this));
            }

            if (agent is Agent a)
            {
                actions.Add(CheckRemovePlant(a));
            }

            return actions;
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            return base.ActionsOnClick(agent);
        }

        protected virtual IAction CheckRemovePlant(Agent agent)
        {
            if (GrowthStage == PlantStage.Harvestable)
            {
                return ActionPool.Get<HarvestAction>(this, Owner == null ? false : Owner != agent.Owner);
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
        
        public void BeWatered()
        {
            if (State == PlantState.Drought)
            {
                State = PlantState.None;
                _waterTime = 0;
            }
        }
    }

    public class TreeItem : PlantItem
    {
        public override bool Walkable => false;
        protected override bool Drought => false;
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

        public override List<(PropConfig, int)> CheckDropItems()
        {
            List<(PropConfig, int)> dropItems = new List<(PropConfig, int)>();
            foreach (var dropItem in Config.dropItems)
            {
                var confg = ConfigReader.GetConfig<PropConfig>(dropItem.id);
                dropItems.Add((confg, dropItem.count));
            }

            return dropItems;
        }
    }
}