using System;
using System.Collections.Generic;
using System.Linq;
using AI;
using GameItem;
using Map;
using UnityEngine;

namespace Citizens
{
    public sealed class JobBoard
    {
        public Dictionary<JobUnitType, Queue<JobUnit>> JobUnits { get; private set; } = new Dictionary<JobUnitType, Queue<JobUnit>>();
        public int Count => JobUnits.Values.Sum(queue => queue.Count);

        public Action<JobUnitType, JobUnit> OnJobUnitAdded { get; set; }

        public void Add(JobUnit jobUnit)
        {
            if (!JobUnits.ContainsKey(jobUnit.Type))
            {
                JobUnits[jobUnit.Type] = new Queue<JobUnit>();
            }
            JobUnits[jobUnit.Type].Enqueue(jobUnit);
            OnJobUnitAdded?.Invoke(jobUnit.Type, jobUnit);
        }

        public JobUnit RequestJob(List<JobUnitType> expectJobTypes)
        {
            foreach (var jobType in expectJobTypes)
            {
                if (JobUnits.TryGetValue(jobType, out var jobQueue))
                {
                    if (jobQueue.Count > 0)
                    {
                        return jobQueue.Dequeue();
                    }
                }
            }
            return null;
        }

        public void Cover(JobUnitType jobUnitType, Queue<JobUnit> newJobUnits)
        {
            if (JobUnits.ContainsKey(jobUnitType))
            {
                JobUnits[jobUnitType] = newJobUnits;
            }
            else
            {
                JobUnits.Add(jobUnitType, newJobUnits);
            }
        }
    }

    public class Tenancy
    {
        public FamilyMember Tenant { get; }
        public Property Property { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public Tenancy(FamilyMember tenant, Property property, DateTime startDate, DateTime endDate)
        {
            Tenant = tenant;
            Property = property;
            StartDate = startDate;
            EndDate = endDate;
        }
    }

    public class Property
    {
        public List<Vector2Int> Blocks { get; } = new List<Vector2Int>();
        public IHouse House { get; private set; }
        public Tenancy CurrentTenancy { get; protected set; }
        public Family Owner { get; private set; }

        public bool IsForSale => CurrentTenancy == null && PropertyManager.I.PropertiesForSale.Contains(this);
        public bool IsForRent => CurrentTenancy == null && PropertyManager.I.PropertiesForRent.Contains(this);

        public Property(IHouse house, Family owner)
        {
            House = house;
            Owner = owner;
            Blocks.AddRange(house.Blocks);
        }

        public void Transfer(Family family)
        {
            family.Properties.Add(this);
            Owner = family;
        }

        public void LeaseTo(FamilyMember member, DateTime startDate, DateTime endDate)
        {
            CurrentTenancy = new Tenancy(member, this, startDate, endDate);
        }
    }

    public abstract class BusinessProperty
    {
        public Property Property { get; private set; }
        public JobBoard JobBoard { get; } = new JobBoard();
        public List<Employee> Employees { get; } = new List<Employee>();
        private float[] _workTime = new float[2] { 8 * 60 * 60, 8 * 60 * 60 };
        public Dictionary<WorkType, int> JobRecruitCount { get; } = new Dictionary<WorkType, int>();
        public City City { get; private set; }

        public virtual void Init(Property property, City city)
        {
            Property = property;
            PropertyManager.I.BusinessProperties.Add(property, this);
            City = city;
        }

        public void AddRecruitCount(WorkType workType, int count = 1)
        {
            if (JobRecruitCount.ContainsKey(workType))
            {
                JobRecruitCount[workType] += count;
            }
            else
            {
                JobRecruitCount.Add(workType, count);
            }
        }

        public void AddApplicant(WorkType workType, Agent agent)
        {
            var employee = Activator.CreateInstance(Type.GetType($"Citizens.{workType}"), agent.Citizen, _workTime) as Employee;
            Employees.Add(employee);
            employee.Property = this;
            agent.Citizen.SetWork(employee);
        }
    }


    public class FarmProperty : BusinessProperty
    {
        public override void Init(Property property, City city)
        {
            base.Init(property, city);
            CheckFarmHoed();
        }

        private void CheckFarmHoed()
        {
            foreach (var pos in Property.Blocks)
            {
                var block = new Vector3(pos.x, pos.y);
                if (MapManager.I.TryGetBuildingItem(block, out var buildingItem) && buildingItem is FarmItem farmItem)
                {
                    CheckPlantToFarm(farmItem);
                }
            }
        }

        private void CheckPlantToFarm(FarmItem farmItem)
        {
            if (farmItem.PlantItem == null)
            {
                var jobUnit = new JobUnit(JobUnitType.Farm, ActionPool.Get<PlantAction>(farmItem, "PROP_SEED_WHEAT"), jobUnit =>
                {
                    Debug.Log($"种植作物：{farmItem.PlantItem.Config.name}");
                    farmItem.PlantItem.OnEventInvoked += OnCropItemEventInvoked;
                });
                JobBoard.Add(jobUnit);
            }
        }

        private void OnCropItemEventInvoked(PlantItem plantItem)
        {
            foreach (var state in plantItem.States)
            {
                if (state == PlantState.Weeds)
                {
                    var jobUnit = new JobUnit(JobUnitType.Farm, ActionPool.Get<WeedingAction>(plantItem), jobUnit =>
                    {
                        Debug.Log($"除草：{plantItem.Config.name}");
                    });
                    JobBoard.Add(jobUnit);
                }
            }

            if (plantItem.GrowthStage == PlantStage.Harvestable)
            {
                var jobUnit = new JobUnit(JobUnitType.Farm, ActionPool.Get<HarvestAction>(plantItem), jobUnit =>
                {
                    Debug.Log($"收获作物：{plantItem.Config.name}");
                });
                JobBoard.Add(jobUnit);
            }
        }
    }

    public class RestaurantProperty : BusinessProperty
    {
        private List<StoveItem> _stoveItems = new List<StoveItem>();

        public override void Init(Property property, City city)
        {
            base.Init(property, city);
            foreach (var furniture in Property.House.FurnitureItems)
            {
                if (furniture.Value is ChairItem chairItem)
                {
                    chairItem.OnSit += (agent) =>
                    {
                        var jobUnit = new JobUnit(JobUnitType.Service, ActionPool.Get<GetOrderAction>(this, agent));
                        JobBoard.Add(jobUnit);
                    };
                }
                else if (furniture.Value is StoveItem stoveItem)
                {
                    _stoveItems.Add(stoveItem);
                }
            }
        }

        public void AddOrder(PropConfig propConfig)
        {
            var stove = _stoveItems.Find(stove => stove.Using == null);
            var jobUnit = new JobUnit(JobUnitType.Cook, ActionPool.Get<CookAction>(stove, propConfig));
            JobBoard.Add(jobUnit);
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
            foreach (var furnitureItem in Property.House.FurnitureItems)
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
        
    }

    public class TavernProperty : RestaurantProperty
    {
        
    }

    public class ShopProperty : BusinessProperty
    {
        private float _restockRate = 0.5f;
        public List<ContainerItem> ContainerItems { get; } = new List<ContainerItem>();
        private List<ShopShelfItem> _shopShelfItems = new List<ShopShelfItem>();

        private List<ShopShelfItem> ShopShelfItemsInRestocking = new List<ShopShelfItem>();
        public override void Init(Property property, City city)
        {
            base.Init(property, city);

            var configs = ConfigReader.GetAllConfigs<PropConfig>(c => c.type == PropType.Seed || c.type == PropType.Crop || c.type == PropType.Ingredient);

            foreach (var furniture in Property.House.FurnitureItems)
            {
                if (furniture.Value is ShopShelfItem shopShelfItem)
                {
                    shopShelfItem.OnSoldEvent += CheckRestock;
                    shopShelfItem.Owner = Property.Owner;
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
                    var jobUnit = new JobUnit(JobUnitType.Sale, ActionPool.Get<RestockAction>(this, shelfItem, propConfig, gap));
                    JobBoard.Add(jobUnit);
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
                    var jobUnit = new JobUnit(JobUnitType.Sale, ActionPool.Get<RestockAction>(this, shelfItem, propConfig, gap));
                    JobBoard.Add(jobUnit);
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