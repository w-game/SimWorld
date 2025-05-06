using System;
using System.Collections.Generic;
using System.Linq;
using AI;
using GameItem;
using Map;
using UnityEngine;

namespace Citizens
{
    public class Property
    {
        public static Dictionary<IHouse, Property> Properties { get; } = new Dictionary<IHouse, Property>();
        public IHouse House { get; private set; }
        public Family Owner { get; private set; }
        public List<Employee> Employees { get; } = new List<Employee>();
        public Dictionary<Type, List<JobUnit>> JobUnits { get; } = new Dictionary<Type, List<JobUnit>>();
        private float[] _workTime = new float[2] { 8 * 60 * 60, 8 * 60 * 60 };
        public Dictionary<JobConfig, int> JobRecruitCount { get; } = new Dictionary<JobConfig, int>();
        public event Action<Type, JobUnit> OnJobUnitAdded;

        public Property(IHouse house, Family owner)
        {
            House = house;
            Owner = owner;

            Properties.Add(house, this);

            BeBought(owner.Members[0].Agent);

            Debug.Log($"Property: {House.HouseType} has been created.");
        }

        protected void AddJobUnit<T>(JobUnit jobUnit) where T : Job
        {
            var type = typeof(T);
            if (!JobUnits.ContainsKey(type))
            {
                JobUnits[type] = new List<JobUnit>();
            }
            JobUnits[type].Add(jobUnit);
            OnJobUnitAdded?.Invoke(type, jobUnit);
        }

        public void AddApplicant(JobConfig jobConfig, Agent agent)
        {
            var employee = Activator.CreateInstance(Type.GetType($"Citizens.{jobConfig.type}"), agent.Citizen, _workTime) as Employee;
            Employees.Add(employee);
            employee.Property = this;
            agent.Citizen.SetJob(employee);
        }

        public void BeBought(Agent agent)
        {
            if (Owner != null)
            {
                foreach (var member in Owner.Members)
                {
                    if (member.IsAdult && member.Job is Owner owner)
                    {
                        owner.RemoveProperty(this);
                        if (owner.Property == null)
                        {
                            member.SetJob(null);
                        }
                    }
                }
            }

            foreach (var member in agent.Owner.Members)
            {
                if (member.IsAdult)
                {
                    var ownerJob = new Owner(member);
                    ownerJob.AddProperty(this);
                    if (member.Job != null)
                    {
                        member.SetJob(ownerJob);
                    }
                }
            }
        }
    }

    public class FarmProperty : Property
    {
        public FarmProperty(IHouse house, Family owner) : base(house, owner)
        {
            CheckFarmHoed();
        }

        private void CheckFarmHoed()
        {
            foreach (var pos in House.Blocks)
            {
                var block = new Vector3(pos.x, pos.y);
                MapManager.I.TryGetBuildingItem(block, out var buildingItem);
                if (buildingItem == null)
                {
                    var jobUnit = new JobUnit(ActionPool.Get<HoeAction>(block, House), jobUnit =>
                    {
                        if (MapManager.I.TryGetBuildingItem(block, out var item))
                        {
                            if (item is FarmItem farmItem)
                            {
                                CheckPlantToFarm(farmItem);
                            }
                        }

                    });
                    AddJobUnit<Farmer>(jobUnit);
                }
                else
                {
                    if (buildingItem is FarmItem farmItem)
                    {
                        CheckPlantToFarm(farmItem);
                    }
                }
            }

            JobRecruitCount.Add(ConfigReader.GetConfig<JobConfig>("JOB_FARMER"), 1);
        }

        private void CheckPlantToFarm(FarmItem farmItem)
        {
            if (farmItem.PlantItem == null)
            {
                var jobUnit = new JobUnit(ActionPool.Get<PlantAction>(farmItem, "PROP_SEED_WHEAT"), jobUnit =>
                {
                    Debug.Log($"种植作物：{farmItem.PlantItem.Config.name}");
                    farmItem.PlantItem.OnEventInvoked += OnCropItemEventInvoked;
                });
                AddJobUnit<Farmer>(jobUnit);
            }
        }

        private void OnCropItemEventInvoked(PlantItem plantItem)
        {
            foreach (var state in plantItem.States)
            {
                if (state == PlantState.Weeds)
                {
                    var jobUnit = new JobUnit(ActionPool.Get<WeedingAction>(plantItem));
                    AddJobUnit<Farmer>(jobUnit);
                }
            }

            if (plantItem.GrowthStage == PlantStage.Harvestable)
            {
                var jobUnit = new JobUnit(ActionPool.Get<HarvestAction>(plantItem));
                AddJobUnit<Farmer>(jobUnit);
            }
        }
    }

    public class RestaurantProperty : Property
    {
        private List<StoveItem> _stoveItems = new List<StoveItem>();
        public RestaurantProperty(IHouse house, Family owner) : base(house, owner)
        {
            foreach (var furniture in House.FurnitureItems)
            {
                if (furniture.Value is ChairItem chairItem)
                {
                    chairItem.OnSit += (agent) =>
                    {
                        var jobUnit = new JobUnit(ActionPool.Get<GetOrderAction>(this, agent));
                        AddJobUnit<Waiter>(jobUnit);
                    };
                }
                else if (furniture.Value is StoveItem stoveItem)
                {
                    _stoveItems.Add(stoveItem);
                }
            }

            JobRecruitCount.Add(ConfigReader.GetConfig<JobConfig>("JOB_COOKER"), 1);
            JobRecruitCount.Add(ConfigReader.GetConfig<JobConfig>("JOB_WAITER"), 1);
        }

        public void AddOrder(PropConfig propConfig)
        {
            var stove = _stoveItems.Find(stove => stove.Using == null);
            var jobUnit = new JobUnit(ActionPool.Get<CookAction>(stove, propConfig));
            AddJobUnit<Cooker>(jobUnit);
        }

        public List<Vector2Int> GetAvailableCommericalPos()
        {
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            // 计算可用的商业位置，避开家具
            // foreach (var pos in House.CommercialPos)
            // {
            //     if (!House.FurnitureItems.ContainsKey(pos))
            //     {
            //         availablePositions.Add(pos);
            //     }
            // }
            return availablePositions;
        }

        public ChairItem GetAvailableSit()
        {
            // 获取可用的座位
            foreach (var furnitureItem in House.FurnitureItems)
            {
                if (furnitureItem.Value is ChairItem chair && chair.Using == null)
                {
                    return chair;
                }
            }
            return null;
        }

        public PropConfig GetOrder(Agent consumer)
        {
            var orders = ConfigReader.GetAllConfigs<PropConfig>();
            if (consumer == GameManager.I.CurrentAgent)
            {
                // TODO: 弹出窗口进行选择
                return orders[0];
            }
            else
            {
                // 暂时随机
                var randomOrder = orders[UnityEngine.Random.Range(0, orders.Count)];
                return randomOrder;
            }
        }
    }

    public class TeahouseProperty : RestaurantProperty
    {
        public TeahouseProperty(IHouse house, Family owner) : base(house, owner)
        {
        }
    }

    public class TavernProperty : RestaurantProperty
    {
        public TavernProperty(IHouse house, Family owner) : base(house, owner)
        {
        }
    }
    
    public class ShopProperty : Property
    {
        private float _restockRate = 0.5f;
        public List<ContainerItem> ContainerItems { get; } = new List<ContainerItem>();
        private List<ShopShelfItem> _shopShelfItems = new List<ShopShelfItem>();

        private List<ShopShelfItem> ShopShelfItemsInRestocking = new List<ShopShelfItem>();

        public ShopProperty(IHouse house, Family owner) : base(house, owner)
        {
            var configs = ConfigReader.GetAllConfigs<PropConfig>(c => c.type == PropType.Seed || c.type == PropType.Crop || c.type == PropType.Ingredient);

            foreach (var furniture in House.FurnitureItems)
            {
                if (furniture.Value is ShopShelfItem shopShelfItem)
                {
                    shopShelfItem.OnSoldEvent += CheckRestock;
                    shopShelfItem.Owner = owner;
                    _shopShelfItems.Add(shopShelfItem);
                }
                else if (furniture.Value is ContainerItem containerItem)
                {
                    ContainerItems.Add(containerItem);

                    for (int i = 0; i < containerItem.Inventory.MaxSize; i++)
                    {
                        var propConfig = configs[UnityEngine.Random.Range(0, configs.Count)];
                        containerItem.AddItem(propConfig, propConfig.maxStackSize);
                    }
                }
            }

            foreach (var shelfItem in _shopShelfItems)
            {
                var propConfig = configs[UnityEngine.Random.Range(0, configs.Count)];
                shelfItem.Restock(propConfig, propConfig.maxStackSize);
            }

            JobRecruitCount.Add(ConfigReader.GetConfig<JobConfig>("JOB_SALESMAN"), 1);
        }

        private void CheckRestock(ShopShelfItem shelfItem, PropConfig propConfig)
        {
            if (ShopShelfItemsInRestocking.Contains(shelfItem))
            {
                return;
            }

            if (shelfItem.SellItem == null)
            {
                var container = ContainerItems.Find(c => c.Inventory.CheckItemAmount(propConfig.id) > 0);
                if (container != null)
                {
                    var remainAmount = container.Inventory.CheckItemAmount(propConfig.id);
                    var gap = remainAmount > propConfig.maxStackSize ? propConfig.maxStackSize : remainAmount;
                    if (gap <= 0)
                    {
                        ShopShelfItemsInRestocking.Remove(shelfItem);
                        return;
                    }
                    var jobUnit = new JobUnit(ActionPool.Get<RestockAction>(this, shelfItem, propConfig, gap));
                    AddJobUnit<Salesman>(jobUnit);
                    ShopShelfItemsInRestocking.Add(shelfItem);
                }
            }
            else
            {
                var gap = shelfItem.SellItem.Config.maxStackSize - shelfItem.SellItem.PropItem.Quantity;
                if (gap <= 0)
                {
                    ShopShelfItemsInRestocking.Remove(shelfItem);
                    return;
                }
                if (shelfItem.SellItem.PropItem.Quantity < shelfItem.SellItem.Config.maxStackSize * _restockRate)
                {
                    var jobUnit = new JobUnit(ActionPool.Get<RestockAction>(this, shelfItem, propConfig, gap));
                    AddJobUnit<Salesman>(jobUnit);
                    ShopShelfItemsInRestocking.Add(shelfItem);
                }
            }
        }

        public void Restock(ShopShelfItem shopShelfItem, PropConfig propConfig, int totalAmount, Agent agent)
        {
            shopShelfItem.Restock(propConfig, totalAmount);
            agent.Bag.RemoveItem(propConfig, totalAmount);
            ShopShelfItemsInRestocking.Remove(shopShelfItem);
            CheckRestock(shopShelfItem, propConfig);
        }
    }
}