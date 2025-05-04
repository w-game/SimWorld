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
            if (_config == null)
            {
                var configs = ConfigReader.GetAllConfigs<PropConfig>();
                var foodConfigs = configs.FindAll(c => c.type == "Food");
                foodConfigs.ForEach(c =>
                {
                    foreach (var material in c.Materials)
                    {
                        var amount = agent.Bag.CheckItemAmount(material.id);
                        if (amount < material.amount)
                        {
                            break;
                        }
                    }

                    _config = c;
                });

                if (_config == null)
                {
                    Done = true;
                    return;
                }
            }

            CheckMoveToArroundPos(agent, _stoveItem, () => { Target = _stoveItem.Pos; });
        }

        protected override void DoExecute(Agent agent)
        {
            foreach (var material in _config.Materials)
            {
                agent.Bag.RemoveItem(ConfigReader.GetConfig<PropConfig>(material.id), material.amount);
            }

            agent.Bag.AddItem(_config, 1);
        }

        public override float Evaluate(Agent agent, HouseType houseType)
        {
            return agent.State.Hunger.CheckState(agent.State.Mood.Value);
        }

        public override void OnGet(params object[] args)
        {
            _stoveItem = args[0] as StoveItem;
            if (args.Length > 1)
                _config = args[1] as PropConfig;

            ActionName = "Cook";

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