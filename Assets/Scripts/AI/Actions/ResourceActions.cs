using System;
using Citizens;
using GameItem;

namespace AI
{
    
    public class CollectResourceAction : SingleActionBase
    {
        private ResourceItem _item;

        public void Execute()
        {

        }

        public override void OnGet(params object[] args)
        {
            _item = args[0] as ResourceItem;
            ActionName = "Collect the resource";
            ActionSpeed = 10f;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _item.Pos);
        }

        protected override void DoExecute(Agent agent)
        {
            foreach (var dropItem in _item.Config.dropItems)
            {
                var confg = GameManager.I.ConfigReader.GetConfig<PropConfig>(dropItem.id);
                var propItem = GameItemManager.CreateGameItem<PropGameItem>(confg, _item.Pos, GameItemType.Static, dropItem.count);
                propItem.ShowUI();
            }

            GameItemManager.DestroyGameItem(_item);
            Done = true;
        }
    }
}