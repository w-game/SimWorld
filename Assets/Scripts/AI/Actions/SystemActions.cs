using Citizens;
using UnityEngine.Events;

namespace AI
{
    public class SystemAction : SingleActionBase
    {
        public SystemAction(string actionName, UnityAction<IAction> callback, IAction action = null) : base(999f)
        {
            ActionName = actionName;
            OnCompleted += (a) => callback?.Invoke(a);
            PrecedingActions.Add(action);
        }

        public override void OnRegister(Agent agent)
        {

        }

        protected override void DoExecute(Agent agent)
        {
        }
    }
}