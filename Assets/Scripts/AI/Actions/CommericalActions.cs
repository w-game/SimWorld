using System.Collections.Generic;
using Citizens;
using GameItem;
using UI.Models;
using UnityEngine;

namespace AI
{
    public class WorkAction : ConditionActionBase
    {
        private Work _job;

        protected override void DoExecute(Agent agent)
        {
            if (agent.Citizen.Work == null)
            {
                Done = true;
                return;
            }

            if (_job.CurJob == null)
            {
                if (_job.AutoAssign)
                    _job.Next();
            }
            else
            {
                _job.CurJob.Done = true;
            }
        }

        public override void OnRegister(Agent agent)
        {
            _job.OnJobUnitAssigned += StartJob;
        }

        private void StartJob()
        {
            _job.CurJob.Action.OnRegister(_job.Member.Agent);
            PrecedingActions.Add(_job.CurJob.Action);
            Debug.Log($"开始工作：{_job.CurJob.Action.ActionName}");
        }

        public override void OnGet(params object[] args)
        {
            _job = args[0] as Work;
            ActionName = "Work";

            Condition = () => GameManager.I.GameTime.CurrentTime < _job.WorkTime[0] || GameManager.I.GameTime.CurrentTime > _job.WorkTime[1];
        }

        public override void OnRelease()
        {
            base.OnRelease();
            _job.OnJobUnitAssigned -= StartJob;
        }
    }

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
        private Property _property;
        private Work _self;

        public override void OnGet(params object[] args)
        {
            _property = args[0] as Property;
            _self = args[1] as Work;
            ActionName = "Wait For Order";
            // Condition = () => _self.JobUnits.Count > 0;
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
    
    public class RestockAction : SingleActionBase
    {
        private ShopProperty _shopProperty;
        private ShopShelfItem _shopShelfItem;
        private PropConfig _propConfig;
        private int _amount;

        private List<(ContainerItem, int)> _containerItems;

        private int _totalAmount;

        public override void OnGet(params object[] args)
        {
            _shopProperty = args[0] as ShopProperty;
            _shopShelfItem = args[1] as ShopShelfItem;
            _propConfig = args[2] as PropConfig;
            _amount = (int)args[3];
            ActionName = "Restock";
            ActionSpeed = 20f;

            _containerItems = new List<(ContainerItem, int)>();
            _totalAmount = 0;
        }

        private bool FindContainerItem()
        {
            _containerItems.Clear();
            int totalAmount = 0;
            foreach (var containerItem in _shopProperty.ContainerItems)
            {
                int amount = containerItem.CheckAmount(_propConfig);
                if (amount <= 0) continue;
                int need = Mathf.Min(amount, _amount - totalAmount);
                _containerItems.Add((containerItem, need));
                totalAmount += need;
                if (totalAmount >= _amount) break;
            }
            return totalAmount > 0;
        }

        public override void OnRegister(Agent agent)
        {
            if (!FindContainerItem())
            {
                ActionFailed();
                return;
            }
            foreach (var (containerItem, need) in _containerItems)
            {
                // Take needed items
                AddPrecedingAction<TakeItemFromContainer>(agent, null, containerItem, _propConfig, need);
                _totalAmount += need;
            }
            // Move back to shelf
            CheckMoveToArroundPos(agent, _shopShelfItem);
        }

        protected override void DoExecute(Agent agent)
        {
            if (_totalAmount <= 0)
            {
                Debug.LogError($"没有足够的物品 {_propConfig.name} 来补货");
                ActionFailed();
            }
            else
            {
                _shopProperty.Restock(_shopShelfItem, _propConfig, _totalAmount, agent);
                _totalAmount = 0;
            }
        }
    }
}