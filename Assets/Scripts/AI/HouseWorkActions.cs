using Citizens;
using GameItem;

namespace AI
{
    public class CookAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 10f;
        public override int ProgressTimes { get; protected set; } = 3;

        private StoveItem _stoveItem;
        private PropConfig _config;

        public CookAction(StoveItem stoveItem, PropConfig config = null)
        {
            _stoveItem = stoveItem;
            _config = config;
        }

        public override float CurrentUtility(AgentState state, float currentMood)
        {
            return state.Hunger.CheckState(currentMood);
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_stoveItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            
        }
    }
}