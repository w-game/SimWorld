using System;
using System.Collections.Generic;
using System.Linq;
using Citizens;
using GameItem;
using Map;
using UnityEngine;

namespace AI
{
    public class PutIntoBag : SingleActionBase
    {
        private PropGameItem _gameItem;

        public override void OnGet(params object[] args)
        {
            _gameItem = args[0] as PropGameItem;
            bool isSteal = (bool)args[1];
            if (isSteal)
            {
                ActionName = "Take (Steal)";
            }
            else
            {
                ActionName = "Take";
            }
            ActionSpeed = 10f;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _gameItem.Pos);
        }

        protected override void DoExecute(Agent agent)
        {
            _gameItem.BePickedUp(agent);
        }
    }

    public class BuyAction : SingleActionBase
    {
        private ShopShelfItem _gameItem;
        private int _amount;

        public override void OnGet(params object[] args)
        {
            _gameItem = args[0] as ShopShelfItem;
            _amount = (int)args[1];
            if (args.Length > 2 && args[2] is bool afford && afford)
            {
                ActionName = $"Buy ({_gameItem.Price} Coins)";
                Enable = true;
            }
            else
            {
                ActionName = "Buy (Not Afford)";
                Enable = false;
            }
            ActionSpeed = 999f;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _gameItem.Pos);
        }

        protected override void DoExecute(Agent agent)
        {
            _gameItem.Buy(agent, _amount);
        }
    }

    public class TakeItemFromContainer : SingleActionBase
    {
        private ContainerItem _containerItem;
        private PropConfig _propConfig;
        private int _amount;

        public override void OnGet(params object[] args)
        {
            _containerItem = args[0] as ContainerItem;
            _propConfig = args[1] as PropConfig;
            _amount = (int)args[2];
            ActionName = $"Take {_propConfig.name}";
            ActionSpeed = 999f;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _containerItem.Pos);
        }

        protected override void DoExecute(Agent agent)
        {
            var propItem = _containerItem.TakeItem(_propConfig, _amount);
            if (propItem != null)
            {
                agent.Bag.AddItem(propItem);
            }
            else
            {
                ActionFailed();
            }
        }
    }

    public class PutIntoContainer : SingleActionBase
    {
        private PropItem _propItem;
        private ContainerItem _containerItem;

        public override void OnGet(params object[] args)
        {
            _propItem = args[0] as PropItem;
            _containerItem = args[1] as ContainerItem;
            ActionName = "Put Into Container";
            ActionSpeed = 999f;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _containerItem);
        }

        protected override void DoExecute(Agent agent)
        {
            agent.Bag.RemoveItem(_propItem.Config, _propItem.Quantity);
            _containerItem.Inventory.AddItem(_propItem);
        }
    }

    public class ShoppingAction : ConditionActionBase
    {
        private Dictionary<string, int> _targetProps;
        private List<IHouse> _shops;
        private IHouse _targetShop;

        public override void OnGet(params object[] args)
        {
            _targetProps = args[0] as Dictionary<string, int>;
            ActionName = "Shopping";
            Condition = () => _targetProps.Count == 0;
        }

        public override void OnRegister(Agent agent)
        {
            var city = MapManager.I.GetCityByPos(agent.Pos);
            if (city == null)
            {
                ActionFailed();
                return;
            }
            _shops = city.Houses.Where(h => h.HouseType == HouseType.Shop).ToList();

            if (_shops.Count == 0 || _targetProps.Count == 0)
            {
                ActionFailed();
                return;
            }

            MoveToNextShop(agent);
        }

        private void MoveToNextShop(Agent agent)
        {
            if (_shops.Count == 0)
            {
                ActionFailed();
                return;
            }

            _targetShop = _shops.OrderBy(h => Vector2.Distance(new Vector2(h.DoorPos.x, h.DoorPos.y), new Vector2(agent.Pos.x, agent.Pos.y))).First();
            _shops.Remove(_targetShop);

            var pos = _targetShop.DoorPos + Vector2Int.up;
            CheckMoveToArroundPos(agent, new Vector3(pos.x, pos.y, 0));
            // PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, new Vector3(pos.x, pos.y, 0)));
        }

        protected override void DoExecute(Agent agent)
        {
            var shelfItems = _targetShop.FurnitureItems.Values.OfType<ShopShelfItem>();

            var matchedItems = shelfItems
                .Where(item => item.SellItem != null && _targetProps.TryGetValue(item.SellItem.Config.id, out _))
                .ToList();

            if (matchedItems.Count > 0)
            {
                shelfItems = matchedItems.OrderBy(item => Vector3.Distance(item.Pos, agent.Pos)).ToList();
                var closestShelf = shelfItems.First();
                var config = closestShelf.SellItem.Config;
                int toBuy = Math.Min(closestShelf.SellItem.PropItem.Quantity, _targetProps[config.id]);
                CheckMoveToArroundPos(agent, closestShelf);
                var buyAction = ActionPool.Get<BuyAction>(closestShelf, toBuy);
                buyAction.OnCompleted += (a, success) =>
                {
                    if (success)
                    {
                        _targetProps[config.id] -= toBuy;
                        if (_targetProps[config.id] <= 0)
                            _targetProps.Remove(config.id);
                    }
                };
                PrecedingActions.Add(buyAction);
            }
            else
            {
                MoveToNextShop(agent);
            }
        }
    }
}