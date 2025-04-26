using System;
using Citizens;
using GameItem;

namespace AI
{
    
    public class CollectResourceAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 1f;
        public override int ProgressTimes { get; protected set; } = -1;

        private ResourceItem _item;

        public CollectResourceAction(ResourceItem item)
        {
            ActionName = "Collect the resource";
            _item = item;
        }

        public void Execute()
        {

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