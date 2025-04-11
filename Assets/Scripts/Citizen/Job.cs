using System;
using System.Collections.Generic;
using AI;
using GameItem;
using Map;
using UnityEngine;
using UnityEngine.Events;

namespace Citizens
{
    public class WorkActionDetector : IActionDetector
    {
        private Job job;

        public WorkActionDetector(Job job)
        {
            this.job = job;
        }

        public List<IAction> DetectActions()
        {
            throw new System.NotImplementedException();
        }
    }

    public class WorkAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 0;
        public override int ProgressTimes { get; protected set; } = -1;

        private Job _job;
        public WorkAction(Job job)
        {
            _job = job;
        }

        public override float CalculateUtility(AgentState state)
        {
            throw new NotImplementedException();
        }

        protected override void DoExecute(AgentState state)
        {
            // 判断工作时间
            if (GameManager.I.GameTime.CurrentTime > _job.WorkTime[0] && GameManager.I.GameTime.CurrentTime < _job.WorkTime[1])
            {
                var jobUnit = _job.CheckJobUnit();
                PrecedingActions.Add(jobUnit.Action);
            }
            else
            {
                Done = true;
            }
        }

        public override void OnRegister(AgentState state)
        {
            
        }
    }
    
    public class Company
    {
        public List<House> Properties { get; } = new List<House>();

        public Family Owner { get; private set; }

        public List<Employee> Employees { get; } = new List<Employee>();

        public void AddEmployee(Employee employee)
        {
            Employees.Add(employee);
        }

        public void AddProperty(House house)
        {
            Properties.Add(house);
            var jobActions = new PropertyToJobActions(house, OnJobUnitCreated);
        }

        private void OnJobUnitCreated(JobUnit jobUnit)
        {
            var employee = Employees[UnityEngine.Random.Range(0, Employees.Count)];
            employee.AddJobUnit(jobUnit);
        }

        public void SetOwner(Family owner)
        {
            Owner = owner;

            foreach (var member in owner.Members)
            {
                if (member.IsAdult)
                {
                    member.SetJob(new Owner());
                }
            }
        }
    }

    public class PropertyToJobActions
    {
        public event UnityAction<JobUnit> OnJobUnitCreated;
        public PropertyToJobActions(House house, UnityAction<JobUnit> onJobUnitCreated)
        {
            OnJobUnitCreated += onJobUnitCreated;
        }
    }

    public class FarmJobActions : PropertyToJobActions
    {
        private Dictionary<Vector2Int, PlantItem> _plantItems = new Dictionary<Vector2Int, PlantItem>();

        public FarmJobActions(House house, UnityAction<JobUnit> onJobUnitCreated) : base(house, onJobUnitCreated)
        {
            foreach (var block in house.Blocks)
            {
                var items = MapManager.I.GetItemsAtPos(new Vector3(block.x, block.y, 0));
                if (items.Count == 0)
                {
                    var jobUnit = new JobUnit(new PlantAction(new Vector3(block.x, block.y, 0), "PROP_SEED_"));
                    onJobUnitCreated?.Invoke(jobUnit);
                    continue;
                }

                foreach (var item in items)
                {
                    if (item is PlantItem plantItem)
                    {
                        plantItem.OnEventInvoked += OnPlantItemEventInvoked;
                        _plantItems.Add(block, plantItem);
                    }
                }
            }
        }

        private void OnPlantItemEventInvoked(PlantItem plantItem)
        {
            // OnJobUnitCreated?.Invoke(new JobUnit(new ));
        }
    }

    public class JobUnit
    {
        public ActionBase Action { get; private set; }
        public JobUnit(ActionBase action)
        {
            Action = action;
        }
    }

    public abstract class Job
    {
        public FamilyMember member { get; set; }
        public float[] WorkTime { get; set; } = new float[2] { 8 * 60 * 60, 18 * 60 * 60 };
        private Queue<JobUnit> _jobUnits { get; set; } = new Queue<JobUnit>();

        public void AddJobUnit(JobUnit jobUnit)
        {
            _jobUnits.Enqueue(jobUnit);
        }

        internal JobUnit CheckJobUnit()
        {
            var jobUnit = _jobUnits.Dequeue();
            return jobUnit;
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

    public class FarmerEmployee : Employee
    {

    }
}