using Citizens;
using UnityEngine.Events;

namespace AI
{
    public class SystemAction : SingleActionBase
    {
        public SystemAction(string actionName, UnityAction<IAction> callback, IAction action = null)
        {
            ActionName = actionName;
            OnCompleted += (a) => callback?.Invoke(a);
            if (action != null)
            {
                PrecedingActions.Add(action);
            }
        }

        public override void OnGet(params object[] args)
        {

        }

        public override void OnRegister(Agent agent)
        {

        }

        protected override void DoExecute(Agent agent)
        {
        }
    }
}