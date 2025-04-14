using System;
using System.Collections.Generic;
using AI;
using GameItem;
using Map;
using UnityEngine;

namespace Citizens
{
    public class Property
    {
        public static Dictionary<House, Property> Properties { get; } = new Dictionary<House, Property>();
        public House House { get; private set; }
        public Family Owner { get; private set; }
        public List<Employee> Employees { get; } = new List<Employee>();
        public List<JobUnit> JobUnits { get; } = new List<JobUnit>();
        private float[] _workTime = new float[2] { 8 * 60 * 60, 18 * 60 * 60 };

        public Property(House house, Family owner)
        {
            House = house;
            Owner = owner;

            Properties.Add(house, this);

            foreach (var member in owner.Members)
            {
                if (member.IsAdult)
                {
                    var ownerJob = new Owner();
                    ownerJob.Property = this;
                    member.SetJob(ownerJob);
                    Schedule schedule = new Schedule(
                        _workTime[0], _workTime[1],
                        new List<int> { 1, 2, 3, 4, 5, 6, 7 },
                        new WorkAction(member.Job), member,
                        SchedulePriority.High
                        );

                    member.Agent.RegisterSchedule(schedule);
                }
            }
        }

        public void AddEmployee(Employee employee)
        {
            Employees.Add(employee);
            employee.Property = this;
            Schedule schedule = new Schedule(
                _workTime[0], _workTime[1],
                new List<int> { 1, 2, 3, 4, 5, 6, 7 },
                new WorkAction(employee), employee.Member
                );
            employee.Member.Agent.RegisterSchedule(schedule);
        }

        private bool AssignJobUnitToOwner(JobUnit jobUnit)
        {
            var owners = Owner.Members.FindAll(member => member.IsAdult && member.Job is Owner);
            if (owners.Count > 0)
            {
                var owner = owners[UnityEngine.Random.Range(0, owners.Count)].Job;
                owner.AssignJobUnit(jobUnit);
                return true;
            }
            return false;
        }

        protected bool AddJobUnit<T>(JobUnit jobUnit) where T : Job
        {
            if (Employees.Count == 0)
            {
                return false;
            }

            var employees = Employees.FindAll(employee => employee is T);
            if (employees.Count == 0)
            {

                return false;
            }

            var employee = employees[UnityEngine.Random.Range(0, employees.Count)];
            employee.AssignJobUnit(jobUnit);
            return true;
        }
    }

    public class FarmProperty : Property
    {
        public FarmProperty(House house, Family owner) : base(house, owner)
        {
            CheckFarmHoed();
        }

        private void CheckFarmHoed()
        {
            foreach (var block in House.Blocks)
            {
                var buildingType = MapManager.I.CheckBuildingType(new Vector3(block.x, block.y, 0));
                if (buildingType != BuildingType.Farm)
                {
                    var jobUnit = new JobUnit(new HoeAction(new Vector3(block.x, block.y, 0)), jobUnit =>
                    {
                        CheckPlantToFarm(block);
                    });
                    AddJobUnit<Farmer>(jobUnit);
                }
            }
        }

        private void CheckPlantToFarm(Vector2Int block)
        {

            var items = GameManager.I.GameItemManager.GetItemsAtPos(new Vector3(block.x, block.y, 0));
            if (items.Count == 0)
            {
                var jobUnit = new JobUnit(new PlantAction(new Vector3(block.x, block.y, 0), "PLANT_WHEAT"), jobUnit =>
                {
                    CheckPlantToFarm(block);
                });
                AddJobUnit<Farmer>(jobUnit);
                return;
            }

            foreach (var item in items)
            {
                if (item is PlantItem plantItem)
                {
                    plantItem.OnEventInvoked += OnCropItemEventInvoked;
                }
            }
        }

        private void OnCropItemEventInvoked(PlantItem plantItem)
        {
            switch (plantItem.State)
            {
                case PlantState.Drought:
                    var jobUnit = new JobUnit(new WaterPlantAction(plantItem.Pos));
                    AddJobUnit<Farmer>(jobUnit);
                    break;
                case PlantState.Weeds:
                    var weedingJobUnit = new JobUnit(new WeedingAction(plantItem));
                    AddJobUnit<Farmer>(weedingJobUnit);
                    break;
                default:
                    break;
            }

            if (plantItem.GrowthStage == PlantStage.Harvestable)
            {
                var jobUnit = new JobUnit(new HarvestAction(plantItem));
                AddJobUnit<Farmer>(jobUnit);
            }
        }
    }

    public class RestaurantProperty : Property
    {
        private List<StoveItem> _stoveItems = new List<StoveItem>();
        public RestaurantProperty(House house, Family owner) : base(house, owner)
        {
            foreach (var furniture in House.FurnitureItems)
            {
                if (furniture.Value is ChairItem chairItem)
                {
                    chairItem.OnSit += (agent) =>
                    {
                        var jobUnit = new JobUnit(new GetOrderAction(this, agent));
                        AddJobUnit<Waiter>(jobUnit);
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
            var jobUnit = new JobUnit(new CookAction(stove, propConfig));
            AddJobUnit<Cooker>(jobUnit);
        }

        public List<Vector2Int> GetAvailableCommericalPos()
        {
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            // 计算可用的商业位置，避开家具
            foreach (var pos in House.CommercialPos)
            {
                if (!House.FurnitureItems.ContainsKey(pos))
                {
                    availablePositions.Add(pos);
                }
            }
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
            var orders = GameManager.I.ConfigReader.GetAllConfigs<PropConfig>();
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
        public TeahouseProperty(House house, Family owner) : base(house, owner)
        {
        }
    }
}