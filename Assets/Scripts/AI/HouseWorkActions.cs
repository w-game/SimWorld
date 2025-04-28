using System.Linq;
using Citizens;
using GameItem;
using Map;
using UnityEngine;

namespace AI
{
    public class CookAction : SingleActionBase
    {
        private StoveItem _stoveItem;
        private PropConfig _config;

        public override void OnRegister(Agent agent)
        {
            if (_stoveItem == null && agent.Citizen.Family.Houses[0].TryGetFurniture<StoveItem>(out var stoveItem))
            {
                _stoveItem = stoveItem;
            }

            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _stoveItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {

        }

        public override float Evaluate(Agent agent, HouseType houseType)
        {
            switch (houseType)
            {
                case HouseType.House:
                    return agent.State.Hunger.CheckState(agent.State.Mood.Value);
            }

            return base.Evaluate(agent, houseType);
        }

        public override void OnGet(params object[] args)
        {
            _stoveItem = args[0] as StoveItem;
            _config = args[1] as PropConfig;

            ActionSpeed = 10f;
        }
    }

    public class OrderFromRestaurant : ConditionActionBase
    {
        private RestaurantProperty _restaurantProperty;

        public override void OnRegister(Agent agent)
        {
            if (_restaurantProperty == null)
            {
                var city = MapManager.I.CartonMap.GetCity(agent.Pos);
                if (city != null)
                {
                    var houses = city.GetHouses(HouseType.Restaurant);
                    if (houses.Count > 0)
                    {
                        var restaurant = houses.OrderBy(a => Vector2.Distance(new Vector2(agent.Pos.x, agent.Pos.y), new Vector2(a.MinPos.x, a.MinPos.y))).First();
                        _restaurantProperty = Property.Properties[restaurant] as RestaurantProperty;
                    }
                }
            }

            var availableCommercialPos = _restaurantProperty.GetAvailableCommericalPos();
            if (availableCommercialPos.Count > 0)
            {
                var pos = availableCommercialPos[Random.Range(0, availableCommercialPos.Count)];
                var moveToTarget = ActionPool.Get<CheckMoveToTarget>(agent, new Vector3(pos.x, pos.y), "Restaurant");
                PrecedingActions.Add(moveToTarget);
                PrecedingActions.Add(ActionPool.Get<WaitForAvailableSitAction>(_restaurantProperty));
            }
        }

        protected override void DoExecute(Agent agent)
        {

        }

        public override float Evaluate(Agent agent, HouseType houseType)
        {
            switch (houseType)
            {
                case HouseType.House:
                    return agent.State.Hunger.CheckState(agent.State.Mood.Value);
                case HouseType.Restaurant:
                    return 100f;
            }

            return base.Evaluate(agent, houseType);
        }

        public override void OnGet(params object[] args)
        {
            _restaurantProperty = args[0] as RestaurantProperty;
        }
    }
}