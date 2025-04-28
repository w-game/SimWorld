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

        public override void OnGet(params object[] args)
        {
            _chairItem = args[0] as ChairItem;
            _consumer = args[1] as Agent;
            ActionName = "Order";
            ActionSpeed = 999f;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _chairItem.Pos));
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

        public override void OnGet(params object[] args)
        {
            _property = args[0] as RestaurantProperty;
            _self = args[1] as Job;
            ActionName = "Wait For Order";
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

        public override void OnGet(params object[] args)
        {
            _property = args[0] as RestaurantProperty;
            _consumer = args[1] as Agent;
            ActionName = "Get Order";
            ActionSpeed = 20f;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _consumer.Pos));
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

        public override void OnGet(params object[] args)
        {
            _property = args[0] as RestaurantProperty;
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