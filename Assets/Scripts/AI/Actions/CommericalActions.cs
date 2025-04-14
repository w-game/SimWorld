using Citizens;
using GameItem;

namespace AI
{
    public class OrderAction : ActionBase
    {
        public override float ProgressSpeed { get => 1.0f; protected set { } }
        public override int ProgressTimes { get => -1; protected set { } }

        private Agent _consumer;
        private ChairItem _chairItem;
        public OrderAction(ChairItem chairItem, Agent consumer)
        {
            _chairItem = chairItem;
            _consumer = consumer;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_chairItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            _chairItem.SitDown(_consumer);
        }
    }
    
    public class WaitForOrderAction : ActionBase
    {
        public override float ProgressSpeed { get => 1.0f; protected set { } }
        public override int ProgressTimes { get => -1; protected set { } }

        private RestaurantProperty _property;
        private Job _self;
        public WaitForOrderAction(RestaurantProperty property, Job self)
        {
            _property = property;
            _self = self;
        }

        public override void OnRegister(Agent agent)
        {

        }

        protected override void DoExecute(Agent agent)
        {
            if (_self.JobUnits.Count > 0)
            {
                Done = true;
            }
        }
    }

    public class GetOrderAction : ActionBase
    {
        public override float ProgressSpeed { get => 20.0f; protected set { } }
        public override int ProgressTimes { get => -1; protected set { } }

        private Agent _consumer;
        private RestaurantProperty _property;
        public GetOrderAction(RestaurantProperty property, Agent consumer)
        {
            _property = property;
            _consumer = consumer;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_consumer.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            var order = _property.GetOrder(_consumer);
            _property.AddOrder(order);
            Done = true;
        }
    }
    
    public class WaitForAvailableSitAction : ActionBase
    {
        public override float ProgressSpeed { get => 1.0f; protected set { } }
        public override int ProgressTimes { get => -1; protected set { } }

        private RestaurantProperty _property;
        public WaitForAvailableSitAction(RestaurantProperty property)
        {
            _property = property;
        }

        public override void OnRegister(Agent agent)
        {

        }

        protected override void DoExecute(Agent agent)
        {
            var chair = _property.GetAvailableSit();
            if (chair != null)
            {
                NextAction = new OrderAction(chair, agent);
                Done = true;
            }
        }
    }
}