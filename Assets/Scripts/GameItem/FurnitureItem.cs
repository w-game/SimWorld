using System;
using System.Collections.Generic;
using System.Linq;
using AI;
using Citizens;
using UI.Models;
using UI.Popups;
using UnityEngine;
using UnityEngine.Events;

namespace GameItem
{
    public class FurnitureItem : GameItemBase<BuildingConfig>
    {
        public Agent Using { get; internal set; }

        public override void Init(BuildingConfig config, Vector3 pos, params object[] args)
        {
            base.Init(config, pos, args);
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

    }

    public class BedItem : FurnitureItem
    {
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

        public override void Init(BuildingConfig config, Vector3 pos, params object[] args)
        {
            base.Init(config, pos, args);
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

    }

    public class HygieneItem : FurnitureItem
    {
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
        public class BulletinData
        {
            public string title;
            public string content;
            public string author;
            public float time;
        }

        public List<BulletinData> Bulletins { get; } = new List<BulletinData>();

        private const float BULLETIN_LIFETIME = 86400f; // seconds – adjust to desired in‑game duration
        private const int   MAX_BULLETINS     = 5;

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

        public override void Update()
        {
            // Implement any specific update logic for bulletin board items here
        }

        private void CheckCreateBulletin()
        {
            // 1. Remove expired bulletins
            Bulletins.RemoveAll(b => Time.time - b.time > BULLETIN_LIFETIME);

            // 2. Only continue if we still have room for new entries
            if (Bulletins.Count >= MAX_BULLETINS) return;

            if (!PropertyManager.I.PropertyRecruits.TryGetValue(MapManager.I.GetCityByPos(Pos), out var workInfos))
                return;

            // 3. Collect candidate (BusinessProperty, WorkType) pairs without repetition
            List<(BusinessProperty, WorkType)> workList = new List<(BusinessProperty, WorkType)>();
            while (workList.Count < MAX_BULLETINS && workList.Count < workInfos.Count)
            {
                var randomProperty = workInfos.ElementAt(UnityEngine.Random.Range(0, workInfos.Count));
                var pair = (randomProperty.Key, randomProperty.Value[UnityEngine.Random.Range(0, randomProperty.Value.Count)]);
                if (!workList.Contains(pair))
                {
                    workList.Add(pair);
                }
            }

            // 4. Create bulletins until the board is full
            foreach (var (property, workType) in workList)
            {
                if (Bulletins.Count >= MAX_BULLETINS) break;

                // Skip if a similar bulletin already exists
                // if (Bulletins.Any(b => b.title.Contains(property.name) && b.content.Contains(workType.ToString())))
                //     continue;
                var data = new BulletinData
                {
                    // author = property.Owner?.Name ?? "佚名",
                    time   = Time.time
                };

                switch (workType)
                {
                    case WorkType.Cooker:
                        data.title   = "招厨榜";
                        data.content = $"本店新张，炉灶方兴，诚聘膳夫一名，精擅百味烹调，供食宿，月俸面议。有意者请至新宇当面洽谈。";
                        break;

                    case WorkType.FarmHelper:
                        data.title   = "招佃示告";
                        data.content = $"本庄坐拥良田五亩，水利便捷，宜耕宜种，今觅勤恳善耕之人租佃。请至城西王员外面议佃租，切勿失良机。";
                        break;

                    case WorkType.Salesman:
                        data.title   = "招伙计启事";
                        data.content = $"本店货源充盈，急招伙计一名，需口齿伶俐，能写会算。月钱优厚，供膳宿，欲者速来。";
                        break;

                    case WorkType.Waiter:
                        data.title   = "招堂倌启事";
                        data.content = $"本店酒客盈门，现募堂倌数名，须手脚利落，待客周到。食宿全包，月银面议。欲试者速至。";
                        break;

                    default:
                        continue; // Skip unsupported work types
                }

                // 防止空标题或重复内容
                // if (string.IsNullOrWhiteSpace(data.title) ||
                //     Bulletins.Any(b => b.title == data.title && b.content == data.content))
                //     continue;

                Bulletins.Add(data);
            }
        }

        public void OnClick()
        {
            CheckCreateBulletin();
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