using Citizens;
using GameItem;

namespace AI
{
    public class OrderAction : SingleActionBase
    {
        private Agent _consumer;
        private ChairItem _chairItem;
        public OrderAction(ChairItem chairItem, Agent consumer)
        {
            _chairItem = chairItem;
            _consumer = consumer;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(agent, _chairItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            _chairItem.SitDown(_consumer);
        }
    }
    
    public class WaitForOrderAction : ConditionActionBase
    {
        private RestaurantProperty _property;
        private Job _self;
        public WaitForOrderAction(RestaurantProperty property, Job self)
        {
            _property = property;
            _self = self;

            Condition = () => _self.JobUnits.Count > 0;
        }

        public override void OnRegister(Agent agent)
        {

        }

        protected override void DoExecute(Agent agent)
        {

        }
    }

    public class GetOrderAction : SingleActionBase
    {
        private Agent _consumer;
        private RestaurantProperty _property;
        public GetOrderAction(RestaurantProperty property, Agent consumer) : base(20f)
        {
            _property = property;
            _consumer = consumer;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(agent, _consumer.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            var order = _property.GetOrder(_consumer);
            _property.AddOrder(order);
        }
    }
    
    public class WaitForAvailableSitAction : ConditionActionBase
    {
        private RestaurantProperty _property;
        private ChairItem _chairItem;
        public WaitForAvailableSitAction(RestaurantProperty property)
        {
            _property = property;
            Condition = () =>
            {
                _chairItem = _property.GetAvailableSit();
                return _chairItem != null;
            };
        }

        public override void OnRegister(Agent agent)
        {

        }

        protected override void DoExecute(Agent agent)
        {
            NextAction = new OrderAction(_chairItem, agent);
        }
    }
}