using Citizens;
using GameItem;

namespace AI
{
    public class ProcessingItemAction : MultiTimesActionBase
    {
        private ProcessingItemBase _item;

        public override void OnGet(params object[] args)
        {
            _item = args[0] as ProcessingItemBase;
            ProgressSpeed = 10f;
            TotalTimes = _item.ConvertionTime;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _item.Pos, () => {
                Target = _item.Pos;
                agent.Bag.RemoveItem(_item.CurItem, 1);
            });
        }

        protected override void DoExecute(Agent agent)
        {
            _item.ProcessItem(CurTime);
        }
    }
}