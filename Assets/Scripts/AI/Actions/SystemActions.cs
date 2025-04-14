using Citizens;
using Map;
using UI;
using UI.Views;

namespace AI
{
    public class ViewHouseDetailsAction : ActionBase
    {
        public override string ActionName => "ViewHouseDetails";

        public override float ProgressSpeed { get => 1f; protected set => throw new System.NotImplementedException(); }
        public override int ProgressTimes { get => -1; protected set => throw new System.NotImplementedException(); }

        private House _house;
        public ViewHouseDetailsAction(House house)
        {
            _house = house;
        }

        protected override void DoExecute(Agent agent)
        {
            PopStack.Instance.Push<PopHouseDetails>("Prefabs/UI/Popups/PopHouseDetails");
        }

        public override void OnRegister(Agent agent)
        {
            
        }
    }
}