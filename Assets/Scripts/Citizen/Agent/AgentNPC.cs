using System.Collections.Generic;
using System.Linq;
using AI;
using UnityEngine;

namespace GameItem
{
    public class AgentNPC : Agent
    {
        public override void Init(ConfigBase config, Vector3 pos, params object[] args)
        {
            base.Init(config, pos, args);
            var foodIds = new string[] { "PROP_FOOD_MANTOU", "PROP_FOOD_STEW" };
            var randFoodId = foodIds[Random.Range(0, foodIds.Length)];
            var randomFood = ConfigReader.GetConfig<PropConfig>(randFoodId);

            Bag.AddItem(randomFood, Random.Range(0, 5));
        }

        public override void ShowUI()
        {
            base.ShowUI();

            UI.Col.enabled = false;
        }

        public IAction CheckShopping()
        {
            if (Citizen.Home == null)
                return null;

            var house = Citizen.Home.House;
            var containers = house.FurnitureItems.Values
                .OfType<ContainerItem>()
                .ToList();
            
            var allProps = containers
                .SelectMany(c => c.Inventory.Items)
                .ToList();
            
            allProps.AddRange(Bag.Items);

            var foods = allProps.Where(p => p.Config.type == PropType.Food);
            if (foods.Count() > 2)
            {
                // 如果有食物，则不需要购物
                return null;
            }

            var propConfigs = ConfigReader.GetAllConfigs<PropConfig>(c => c.type == PropType.Food);

            // 如果有一种食物，其所有原料都已经具备，则不需要购物
            var targetFoodIds = new[] { "PROP_FOOD_MANTOU", "PROP_FOOD_STEW" };

            var alreadyHasFood = targetFoodIds.Any(foodId =>
            {
                var foodConfig = ConfigReader.GetConfig<PropConfig>(foodId);
                var neededMaterials = foodConfig.Materials.Where(m =>
                {
                    var materialConfig = ConfigReader.GetConfig<PropConfig>(m.id);
                    return materialConfig.type == PropType.Crop || materialConfig.type == PropType.Ingredient;
                }).ToList();
                return neededMaterials.All(m => allProps.Any(p => p.Config.id == m.id && p.Quantity >= m.amount));
            });

            if (alreadyHasFood)
            {
                return null;
            }
            else
            {
                var foodIds = new string[] { "PROP_FOOD_MANTOU", "PROP_FOOD_STEW" };
                var randFoodId = foodIds[Random.Range(0, foodIds.Length)];
                var propConfig = ConfigReader.GetConfig<PropConfig>(randFoodId);
                var allMaterialConfigs = new Dictionary<string, int>();
                foreach (var material in propConfig.Materials)
                {
                    var materialConfig = ConfigReader.GetConfig<PropConfig>(material.id);
                    if (materialConfig.type == PropType.Crop || materialConfig.type == PropType.Ingredient)
                        allMaterialConfigs.Add(material.id, material.amount);
                }
                var action = ActionPool.Get<ShoppingAction>(allMaterialConfigs);

                var returnPos = new Vector3(house.DoorPos.x, house.DoorPos.y, 0);
                action.NextAction = ActionPool.Get<CheckMoveToTarget>(this, returnPos);

                return action;
            }
        }
    }
}