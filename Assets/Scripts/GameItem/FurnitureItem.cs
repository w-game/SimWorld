using System.Collections.Generic;
using System.Linq;
using AI;
using Citizens;
using UI.Models;
using UnityEngine;
using UnityEngine.Events;

namespace GameItem
{
    public class FurnitureItem : GameItemBase<BuildingConfig>
    {
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
                ActionPool.Get<SleepAction>(this),
            };
        }
    }

    public class TableItem : FurnitureItem
    {
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

    public class ShopShelfItem : FurnitureItem, ISelectItem
    {

        public SellItem SellItem { get; private set; }
        public int Price { get; private set; } = 1;
        public int SellAmount { get; private set; } = 1;

        public PropType PropType => PropType.None;

        public event UnityAction<ShopShelfItem, PropConfig> OnSoldEvent;
        public ShopShelfItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {

        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            var actions = new List<IAction>();

            if (Owner != agent.Owner)
            {
                actions.Add(ActionPool.Get<BuyAction>(this, agent.Money.Amount >= Price));
            }

            if (SellItem != null)
            {
                return actions;
            }

            var system = new SystemAction("Select items to sell", a =>
            {
                var model = IModel.GetModel<PopSelectSeedModel>(this);
                model.ShowUI();
            });

            return new List<IAction>()
            {
                system
            };
        }

        public void OnSelected(string id, int amount = 1)
        {
            var agent = GameManager.I.CurrentAgent;
            if (Vector3.Distance(agent.Pos, Pos) > 1)
            {
                agent.MoveToArroundPos(this, () =>
                {
                    Restock(id, amount, agent);
                });
            }
            else
            {
                Restock(id, amount, agent);
            }

        }

        public void OnSold()
        {
            if (SellItem != null)
            {
                GameItemManager.DestroyGameItem(SellItem);
                OnSoldEvent?.Invoke(this, SellItem.Config);

                if (SellItem.PropItem.Quantity == 0)
                    SellItem = null;
            }
        }

        public void Buy(Agent agent)
        {
            if (SellItem != null)
            {
                agent.Bag.AddItem(SellItem.Config, SellAmount);
                agent.Money.Subtract(Price);
                OnSold();
            }
        }

        public void OnTaked(bool isSteal)
        {
            SellItem = null;
        }

        public void Restock(PropConfig config, int amount, Agent agent)
        {
            if (SellItem != null)
            {
                return;
            }

            agent.Bag.RemoveItem(config, amount);
            SellItem = GameItemManager.CreateGameItem<SellItem>(
                config,
                Pos,
                GameItemType.Static,
                amount,
                this);
            SellItem.Owner = Owner;
            SellItem.Pos += new Vector3(0.5f, 0.49f);
            Price = GameManager.I.PriceSystem.GetPrice(MapManager.I.GetCityByPos(Pos), config.id);
            SellItem.UI.SetName($"{config.name} ${Price}");
        }

        public void Restock(string id, int amount, Agent agent)
        {
            Restock(ConfigReader.GetConfig<PropConfig>(id), amount, agent);
        }
    }

    public class BucketItem : FurnitureItem
    {
        public BucketItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            return new List<IAction>()
            {
                // system
            };
        }
    }

    public class ContainerItem : FurnitureItem
    {

        public Inventory Inventory { get; private set; }
        public ContainerItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
            var capacity = config.additionals["capacity"] as int? ?? 10;
            Inventory = new Inventory(capacity);
            var configs = ConfigReader.GetAllConfigs<PropConfig>();
            foreach (var propConfig in configs)
            {
                if (propConfig.type != PropType.Seed.ToString())
                    continue;
                Inventory.AddItem(propConfig, propConfig.maxStackSize);
            }
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            return new List<IAction>()
            {
                // system
            };
        }

        internal PropItem TakeItem(PropConfig propConfig, int amount)
        {
            var propItem = Inventory.Items.Find(i => i.Config == propConfig);
            if (propItem != null)
            {
                Inventory.RemoveItem(propConfig, amount);
                return new PropItem(propConfig, amount);
            }

            return null;
        }

        public int CheckAmount(PropConfig propConfig)
        {
            var propItems = Inventory.Items.FindAll(i => i.Config == propConfig);
            if (propItems.Count > 0)
            {
                return propItems.Sum(i => i.Quantity);
            }

            return 0;
        }
    }

    public class CounterItem : FurnitureItem
    {
        public CounterItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }
    }

    public class HygieneItem : FurnitureItem
    {
        public HygieneItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            return new List<IAction>()
            {
                // system
            };
        }
    }

    public class BasinItem : HygieneItem
    {
        public BasinItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            return new List<IAction>()
            {
                // system
            };
        }
    }
}