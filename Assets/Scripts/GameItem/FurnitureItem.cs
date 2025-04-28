using System;
using System.Collections.Generic;
using AI;
using Citizens;
using UI.Models;
using UnityEngine;
using UnityEngine.Events;

namespace GameItem
{
    public class FurnitureItem : GameItemBase<BuildingConfig>
    {
        public override bool Walkable => true;
        public Agent Using { get; internal set; }

        public FurnitureItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
            Size = new Vector2Int(config.size[0], config.size[1]);
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>()
            {

            };
        }

        public override void ShowUI()
        {
            base.ShowUI();

            UI.SetRenderer(Config.icon);
        }

        public override void Update()
        {
            // Implement any specific update logic for furniture items here
        }
    }

    public class ToiletItem : FurnitureItem
    {
        public ToiletItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }
    }

    public class BedItem : FurnitureItem
    {
        public BedItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            return new List<IAction>()
            {
                ActionPool.Get<SleepAction>(GameManager.I.CurrentAgent.State.Sleep, this),
            };
        }
    }

    public class TableItem : FurnitureItem
    {
        public override bool Walkable => false;
        public List<ChairItem> Chairs { get; } = new List<ChairItem>();

        public TableItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public ChairItem GetChair()
        {
            return Chairs[0];
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>()
            {
            };
        }
    }

    public class ChairItem : FurnitureItem
    {
        public event UnityAction<Agent> OnSit;
        public ChairItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>()
            {

            };
        }

        internal void SitDown(Agent agent)
        {
            Using = agent;
            OnSit?.Invoke(agent);
        }
    }

    public class WellItem : FurnitureItem
    {
        public override bool Walkable => false;
        public WellItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>()
            {
                ActionPool.Get<DrawWaterAction>(this),
            };
        }
    }

    public class StoveItem : FurnitureItem
    {
        public override bool Walkable => false;
        public StoveItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }


        public void SetUsing(Agent agent)
        {
            Using = agent;
        }
    }

    public class WorkbenchItem : FurnitureItem
    {
        public override bool Walkable => false;
        public WorkbenchItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {

        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            List<Vector3> availablePos = new List<Vector3>();
            Vector3 centerPos = Vector3.zero;
            foreach (var pos in OccupiedPositions)
            {
                var checkPos = new Vector3(pos.x, pos.y - 1) + Pos;
                var items = GameManager.I.GameItemManager.GetItemsAtPos(checkPos);
                if (items.Count == 0)
                {
                    availablePos.Add(checkPos);
                    centerPos += checkPos;
                }
            }

            if (availablePos.Count != 0)
            {
                centerPos /= availablePos.Count;
                var action = ActionPool.Get<CheckMoveToTarget>(GameManager.I.CurrentAgent, centerPos);
                var system = new SystemAction("Craft Item", a =>
                {
                    var model = new PopCraftModel();
                    model.ShowUI();
                }, action);

                return new List<IAction>()
                {
                    system
                };
            }


            return new List<IAction>()
            {

            };
        }
    }

    public class SeedIncubatorItem : FurnitureItem
    {
        public override bool Walkable => false;
        public PropItem CultivatingItem { get; private set; }
        // TODO: 根据温度计算速度
        public float Temperature { get; private set; }
        public float CurProgress { get; private set; }

        public event UnityAction<PropConfig> OnFinish;
        public event UnityAction<float> OnProgress;
        public event UnityAction OnChange;
        public bool Done { get; private set; } = false;

        public PropConfig Seed { get; private set; }
        public SeedIncubatorItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            var centerPos = Pos - new Vector3(0, 1);
            var action = ActionPool.Get<CheckMoveToTarget>(GameManager.I.CurrentAgent, centerPos);
            var system = new SystemAction("Craft Item", a =>
            {
                var model = IModel.GetModel<PopSeedIncubatorModel>(this);
                model.ShowUI();
            }, action);

            return new List<IAction>()
            {
                system
            };
        }

        public void ChangeItem(PropItem propItem)
        {
            if (propItem == null)
            {
                return;
            }

            CultivatingItem = propItem;
            var seed = ConfigReader.GetConfig<CropSeedConfig>(CultivatingItem.Config.id).target;
            Seed = ConfigReader.GetConfig<PropConfig>(seed);
            CurProgress = 0;
            Done = false;
            OnChange?.Invoke();
        }

        public override void Update()
        {
            if (CultivatingItem != null && !Done)
            {
                CurProgress += GameTime.DeltaTime;
                OnProgress?.Invoke(CurProgress);

                if (CurProgress >= 100f)
                {
                    OnFinish?.Invoke(Seed);
                    Done = true;
                }
            }
        }

        public void BeTake(Inventory inventory)
        {
            if (CultivatingItem != null)
            {
                inventory.AddItem(new PropItem(Seed, 1));
                CultivatingItem = null;
                Seed = null;
                CurProgress = 0;
                Done = false;
                OnChange?.Invoke();
            }
        }
    }

    public class ShopShelfItem : FurnitureItem
    {
        public override bool Walkable => false;
        public ShopShelfItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
            var configs = ConfigReader.GetAllConfigs<PropConfig>();
            var randomConfig = configs[UnityEngine.Random.Range(0, configs.Count)];
            GameItemManager.CreateGameItem<FoodItem>(
                randomConfig,
                pos,
                GameItemType.Static,
                1);
        }

        // public override List<IAction> ActionsOnClick(Agent agent)
        // {
        //     var centerPos = Pos - new Vector3(0, 1);
        //     var action = ActionPool.Get<CheckMoveToTarget>(GameManager.I.CurrentAgent, centerPos);
        //     var system = new SystemAction("Craft Item", a =>
        //     {
        //         var model = new PopShopModel();
        //         model.ShowUI();
        //     }, action);

        //     return new List<IAction>()
        //     {
        //         system
        //     };
        // }
    }
}