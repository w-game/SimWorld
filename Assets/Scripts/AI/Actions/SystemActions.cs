using Citizens;
using UnityEngine.Events;

namespace AI
{
    public class SystemAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 0;
        public override int ProgressTimes { get; protected set; } = -1;

        public SystemAction(string actionName, UnityAction<IAction> action)
        {
            ActionName = actionName;
            OnCompleted += (a) => action?.Invoke(a);
        }

        public override void OnRegister(Agent agent)
        {

        }

        protected override void DoExecute(Agent agent)
        {
            Done = true;
        }
    }
}