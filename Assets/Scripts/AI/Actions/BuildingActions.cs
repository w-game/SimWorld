using Citizens;
using GameItem;

namespace AI
{
    public class MillstoneAction : MultiTimesActionBase
    {
        private MillstoneItem _millstoneItem;

        public override void OnGet(params object[] args)
        {
            _millstoneItem = args[0] as MillstoneItem;
            ProgressSpeed = 10f;
            TotalTimes = _millstoneItem.ConvertionTime;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _millstoneItem.Pos, () => {
                Target = _millstoneItem.Pos;
                agent.Bag.RemoveItem(_millstoneItem.CurItem, 1);
            });
        }

        protected override void DoExecute(Agent agent)
        {
            _millstoneItem.ProcessItem(CurTime, agent);
        }
    }
}