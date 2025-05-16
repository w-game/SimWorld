using System;
using System.Collections.Generic;
using System.Linq;
using AI;
using UI.Models;
using UI.Popups;
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

        public virtual void DrawWater(PropConfig propConfig)
        {
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

        public override List<IAction> ItemActions(IGameItem agent)
        {
            if (agent is Agent a && a.Owner == Owner)
            {
                return new List<IAction>()
                {
                    ActionPool.Get<CookAction>(this)
                };
            }

            return new List<IAction>();
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

        public PropType PropType => PropType.None;

        public event UnityAction<ShopShelfItem, PropConfig> OnSoldEvent;
        public ShopShelfItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {

        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            var actions = new List<IAction>();

            if (Owner != agent.Owner && SellItem != null)
            {
                actions.Add(ActionPool.Get<BuyAction>(this, SellItem.PropItem.Quantity, agent.Money.Amount >= Price));
            }

            if (SellItem != null)
            {
                return actions;
            }

            var system = new SystemAction("Select items to sell", a =>
            {
                var model = IModel.GetModel<PopSelectSeedModel>();
                model.ShowUI(this);
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
                    Restock(id, amount);
                    agent.Bag.RemoveItem(ConfigReader.GetConfig<PropConfig>(id), amount);
                });
            }
            else
            {
                Restock(id, amount);
                agent.Bag.RemoveItem(ConfigReader.GetConfig<PropConfig>(id), amount);
            }

        }

        public void OnSold(int amount)
        {
            if (SellItem != null)
            {
                SellItem.AddCount(-amount);
                OnSoldEvent?.Invoke(this, SellItem.Config);

                if (SellItem.PropItem.Quantity <= 0)
                {
                    SellItem = null;
                }
            }
        }

        public void Buy(Agent agent, int amount)
        {
            if (SellItem != null)
            {
                if (agent.Bag.AddItem(SellItem.Config, amount))
                {
                    agent.Money.Subtract(Price);
                    OnSold(amount);
                }
            }
        }

        public void OnTaked(bool isSteal)
        {
            SellItem = null;
        }

        public void Restock(PropConfig config, int amount)
        {
            if (SellItem != null)
            {
                amount = amount > config.maxStackSize ? config.maxStackSize : amount;
                SellItem.AddCount(amount);
                return;
            }

            SellItem = GameItemManager.CreateGameItem<SellItem>(
                config,
                Pos,
                GameItemType.Static,
                amount,
                this);
            SellItem.Owner = Owner;
            SellItem.Pos += new Vector3(0.5f, 0.49f);
            Price = GameManager.I.PriceSystem.GetPrice(MapManager.I.GetCityByPos(Pos), config.id);
            SellItem.UI.SetName($"${Price}");
        }

        public void Restock(string id, int amount)
        {
            Restock(ConfigReader.GetConfig<PropConfig>(id), amount);
        }
    }

    public class BucketItem : WellItem
    {
        public int WaterQuantity { get; private set; } = 0;
        public int MaxWaterQuantity { get; private set; } = 5;
        public BucketItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            if (agent.Owner != Owner) return new List<IAction>();
            if (WaterQuantity >= MaxWaterQuantity) return new List<IAction>();
            var city = MapManager.I.GetCityByPos(Pos);
            if (city == null)
                return new List<IAction>();

            return new List<IAction>()
            {
                ActionPool.Get<DrawWaterAction>(city.WellItem, this),
            };
        }

        public void AddWater(int v)
        {
            WaterQuantity = Math.Min(WaterQuantity + v, MaxWaterQuantity);
        }

        public override void DrawWater(PropConfig propConfig)
        {
            WaterQuantity = Math.Max(WaterQuantity - 1, 0);
        }
    }

    public class ContainerItem : FurnitureItem
    {

        public Inventory Inventory { get; private set; }
        public ContainerItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
            if (int.TryParse(config.additionals["capacity"].ToString(), out var capacity))
            {
                Inventory = new Inventory(capacity);
            }
            else
            {
                Inventory = new Inventory(10);
            }
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            if (agent is Agent a)
            {
                var system = new SystemAction("Open Container", a =>
                {
                    var model = IModel.GetModel<PopBagModel>();
                    model.ShowUI(this);
                });

                return new List<IAction>()
                {
                    system
                };
            }
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

        public void AddItem(PropConfig propConfig, int amount)
        {
            Inventory.AddItem(propConfig, amount);
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

    public class BulletinBoardItem : FurnitureItem
    {
        public BulletinBoardItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public override void ShowUI()
        {
            if (UI == null)
            {
                UI = GameManager.I.GameItemManager.ItemUIPool.Get<BulletinBoardItemUI>(Config.prefab, Pos + new Vector3(0.5f, 0.5f, 0));
                UI.Init(this);
            }
            
            UI.SetRenderer(Config.icon);
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            return new List<IAction>()
            {
                // system
            };
        }

        public void OnClick()
        {
            if ((Pos - GameManager.I.CurrentAgent.Pos).sqrMagnitude > 4)
            {
                GameManager.I.CurrentAgent.MoveToArroundPos(this, () =>
                {
                    var model = IModel.GetModel<PopBulletinBoardModel>();
                    model.ShowUI(this);
                });
            }
            else
            {
                var model = IModel.GetModel<PopBulletinBoardModel>();
                model.ShowUI(this);
            }
        }
    }
}