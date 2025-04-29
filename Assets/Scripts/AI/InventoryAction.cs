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
            agent.Bag.AddItem(_gameItem);
            GameItemManager.DestroyGameItem(_gameItem);
        }
    }
}