using System;
using System.Collections.Generic;
using AI;
using GameItem;
using Map;
using UnityEngine;
using UnityEngine.Events;

namespace Citizens
{
    public class WorkAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 0;
        public override int ProgressTimes { get; protected set; } = -1;

        private Job _job;
        private JobUnit _curJobUnit;
        public WorkAction(Job job)
        {
            _job = job;
            ActionName = "Work";
        }

        protected override void DoExecute(Agent agent)
        {
            // 判断工作时间
            if (GameManager.I.GameTime.CurrentTime > _job.WorkTime[0] && GameManager.I.GameTime.CurrentTime < _job.WorkTime[1])
            {
                if (_curJobUnit != null)
                {
                    _curJobUnit.Done = true;
                }
                _curJobUnit = _job.CheckJobUnit();
                if (_curJobUnit == null)
                {
                    return;
                }
                Debug.Log($"开始工作：{_curJobUnit.Action.ActionName}");
                _curJobUnit.Action.OnRegister(agent);
                PrecedingActions.Add(_curJobUnit.Action);
            }
            else
            {
                Done = true;
            }
        }

        public override void OnRegister(Agent agent)
        {

        }
    }

    public class Company
    {
        public string CompanyName { get; private set; }
        public Dictionary<House, PropertyToJobActions> Properties { get; } = new Dictionary<House, PropertyToJobActions>();

        public Family Owner { get; private set; }
        public List<Employee> Employees { get; } = new List<Employee>();
        private Queue<JobUnit> _jobUnits { get; set; } = new Queue<JobUnit>();
        public int RecriutmentCount { get; set; } = 1;

        private float[] _workTime = new float[2] { 8 * 60 * 60, 18 * 60 * 60 };

        public Company(Family owner, string companyName = "Company")
        {
            Owner = owner;
            CompanyName = companyName + UnityEngine.Random.Range(0, 1000);

            foreach (var member in owner.Members)
            {
                if (member.IsAdult)
                {
                    var ownerJob = new Owner();
                    ownerJob.Company = this;
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

        public JobUnit GetJobUnit()
        {
            if (_jobUnits.Count == 0)
            {
                return null;
            }
            var jobUnit = _jobUnits.Dequeue();
            return jobUnit;
        }

        public void AddEmployee(Employee employee)
        {
            Employees.Add(employee);
            employee.Company = this;
            Schedule schedule = new Schedule(
                _workTime[0], _workTime[1],
                new List<int> { 1, 2, 3, 4, 5, 6, 7 },
                new WorkAction(employee), employee.Member
                );
            employee.Member.Agent.RegisterSchedule(schedule);
        }

        public void AddProperty(House house)
        {
            if (house.HouseType == HouseType.Farm)
            {
                var farmJobActions = new FarmJobActions(house, OnJobUnitCreated);
                Properties.Add(house, farmJobActions);
            }
        }

        private void OnJobUnitCreated(JobUnit jobUnit)
        {
            _jobUnits.Enqueue(jobUnit);
        }
    }

    public abstract class PropertyToJobActions
    {
        public event UnityAction<JobUnit> OnJobUnitCreated;
        public House House { get; private set; }
        public PropertyToJobActions(House house, UnityAction<JobUnit> onJobUnitCreated)
        {
            OnJobUnitCreated += onJobUnitCreated;
            House = house;
        }
    }

    public class FarmJobActions : PropertyToJobActions
    {
        private Dictionary<Vector2Int, PlantItem> _plantItems = new Dictionary<Vector2Int, PlantItem>();

        public FarmJobActions(House house, UnityAction<JobUnit> onJobUnitCreated) : base(house, onJobUnitCreated)
        {
            foreach (var block in house.Blocks)
            {
                if (StepOne(block, onJobUnitCreated))
                {
                    continue;
                }

                if (StepTwo(block, onJobUnitCreated))
                {
                    continue;
                }

                StepThree(block, onJobUnitCreated);
            }

            GameManager.I.GameTime.Register(6 * 60 * 60, () =>
            {
                foreach (var block in house.Blocks)
                {
                    var jobUnit = new JobUnit(new WaterPlantAction(new Vector3(block.x, block.y, 0)), onJobUnitFailed: jobUnit =>
                    {
                        jobUnit.Refresh();
                        onJobUnitCreated?.Invoke(jobUnit);
                    });
                    onJobUnitCreated?.Invoke(jobUnit);
                }
            });
        }

        private bool StepOne(Vector2Int block, UnityAction<JobUnit> onJobUnitCreated)
        {
            var buildingType = MapManager.I.CheckBuildingType(new Vector3(block.x, block.y, 0));
            if (buildingType != BuildingType.Farm)
            {
                var jobUnit = new JobUnit(new HoeAction(new Vector3(block.x, block.y, 0)), jobUnit =>
                {
                    StepTwo(block, onJobUnitCreated);
                });
                onJobUnitCreated?.Invoke(jobUnit);

                return true;
            }

            return false;
        }

        private bool StepTwo(Vector2Int block, UnityAction<JobUnit> onJobUnitCreated)
        {
            var items = GameManager.I.GameItemManager.GetItemsAtPos(new Vector3(block.x, block.y, 0));
            if (items.Count == 0)
            {
                var jobUnit = new JobUnit(new PlantAction(new Vector3(block.x, block.y, 0), "PLANT_WHEAT"), jobUnit =>
                {
                    StepThree(block, onJobUnitCreated);
                });
                onJobUnitCreated?.Invoke(jobUnit);
                return true;
            }

            return false;
        }

        private void StepThree(Vector2Int block, UnityAction<JobUnit> onJobUnitCreated)
        {
            var items = GameManager.I.GameItemManager.GetItemsAtPos(new Vector3(block.x, block.y, 0));
            foreach (var item in items)
            {
                if (item is PlantItem plantItem)
                {
                    plantItem.OnEventInvoked += plantItem =>
                    {
                        // var jobUnit = new JobUnit(new HarvestAction(plantItem));
                        // onJobUnitCreated?.Invoke(jobUnit);
                    };
                    _plantItems.Add(block, plantItem);
                }
            }
        }
    }

    public class JobUnit
    {
        public ActionBase Action { get; private set; }
        public event UnityAction<JobUnit> OnJobUnitDone;
        private bool _isDone;
        public bool Done
        {
            get => _isDone;
            set
            {
                _isDone = value;
                if (_isDone)
                {
                    OnJobUnitDone?.Invoke(this);
                }
            }
        }

        public JobUnit(ActionBase action, UnityAction<JobUnit> onJobUnitDone = null, UnityAction<JobUnit> onJobUnitFailed = null)
        {
            OnJobUnitDone += onJobUnitDone;
            Action = action;
            Action.OnActionFailed += (action) =>
            {
                onJobUnitFailed?.Invoke(this);
            };
        }

        internal void Refresh()
        {
            Done = false;
            Action.Reset();
        }
    }

    public abstract class Job
    {
        public FamilyMember Member { get; set; }
        public float[] WorkTime { get; set; } = new float[2] { 0 * 60 * 60, 18 * 60 * 60 };

        public Company Company { get; set; }

        internal JobUnit CheckJobUnit()
        {
            return Company.GetJobUnit();
        }
    }

    public class Owner : Job
    {

    }

    public class Employee : Job
    {
        public int WorkDays { get; set; } = 5;
        public int Salary { get; set; } = 1000;
    }
}