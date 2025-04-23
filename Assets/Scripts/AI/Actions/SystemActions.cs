using Citizens;
using Map;
using UI.Models;

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
            var model = IModel.GetModel<PopHouseDetailsModel>(_house);
            model.ShowUI();
            Done = true;
        }

        public override void OnRegister(Agent agent)
        {
            
        }
    }
}