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

        public CookAction(StoveItem stoveItem = null, PropConfig config = null) : base(10f)
        {
            _stoveItem = stoveItem;
            _config = config;
        }

        public override void OnRegister(Agent agent)
        {
            if (_stoveItem == null && agent.Citizen.Family.Houses[0].TryGetFurniture<StoveItem>(out var stoveItem))
            {
                _stoveItem = stoveItem;
            }

            PrecedingActions.Add(new CheckMoveToTarget(agent, _stoveItem.Pos));
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
    }

    public class OrderFromRestaurant : ConditionActionBase
    {
        private RestaurantProperty _restaurantProperty;

        public OrderFromRestaurant(RestaurantProperty restaurantProperty = null)
        {
            _restaurantProperty = restaurantProperty;
        }

        public override void OnRegister(Agent agent)
        {
            if (_restaurantProperty == null)
            {
                var city = MapManager.I.CartonMap.GetCity(agent.Pos);
                if (city != null)
                {
                    var houses = city.GetHouses(Map.HouseType.Restaurant);
                    if (houses.Count > 0)
                    {
                        var restaurant = houses.OrderBy(a => a.DistanceTo(agent.Pos)).First();
                        _restaurantProperty = Property.Properties[restaurant] as RestaurantProperty;
                    }
                }
            }

            var availableCommercialPos = _restaurantProperty.GetAvailableCommericalPos();
            if (availableCommercialPos.Count > 0)
            {
                var pos = availableCommercialPos[Random.Range(0, availableCommercialPos.Count)];
                var moveToTarget = new CheckMoveToTarget(agent, new Vector3(pos.x, pos.y), "Restaurant");
                PrecedingActions.Add(moveToTarget);
                PrecedingActions.Add(new WaitForAvailableSitAction(_restaurantProperty));
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
    }
}