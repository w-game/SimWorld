using Citizens;
using GameItem;

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

        public override void OnGet(params object[] args)
        {
            _gameItem = args[0] as ShopShelfItem;
            if (args[1] is bool afford && afford)
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
            _gameItem.Buy(agent);
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
}