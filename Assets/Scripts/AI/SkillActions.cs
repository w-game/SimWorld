using Citizens;

namespace AI
{
    public class ReadBookAction : MultiTimesActionBase
    {
        public override void OnGet(params object[] args)
        {
            ActionName = "Read Book";
            ProgressSpeed = 10f;
            TotalTimes = 5;
        }

        public override void OnRegister(Agent agent)
        {
            throw new System.NotImplementedException();
        }

        protected override void DoExecute(Agent agent)
        {
            throw new System.NotImplementedException();
        }
    }
}