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
}