using System;
using System.Collections.Generic;
using AI;
using GameItem;
using Map;
using UnityEngine;
using UnityEngine.Events;

namespace Citizens
{
    public class WorkAction : ConditionActionBase
    {
        private Job _job;
        private JobUnit _curJobUnit;
        public WorkAction(Job job)
        {
            _job = job;
            ActionName = "Work";

            Condition = () => GameManager.I.GameTime.CurrentTime < _job.WorkTime[0] || GameManager.I.GameTime.CurrentTime > _job.WorkTime[1];
        }

        protected override void DoExecute(Agent agent)
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

        public override void OnRegister(Agent agent)
        {

        }
    }

    // public abstract class PropertyToJobActions
    // {
    //     public event UnityAction<JobUnit> OnJobUnitCreated;
    //     public IHouse House { get; private set; }
    //     public PropertyToJobActions(IHouse house, UnityAction<JobUnit> onJobUnitCreated)
    //     {
    //         OnJobUnitCreated += onJobUnitCreated;
    //         House = house;
    //     }
    // }

    // public class FarmJobActions : PropertyToJobActions
    // {
    //     private List<PlantItem> _plantItems = new List<PlantItem>();

    //     public FarmJobActions(IHouse house, UnityAction<JobUnit> onJobUnitCreated) : base(house, onJobUnitCreated)
    //     {
    //         foreach (var pos in house.Blocks)
    //         {
    //             var block = new Vector3(pos.x, pos.y, 0);
    //             if (StepOne(block, onJobUnitCreated))
    //             {
    //                 continue;
    //             }

    //             if (StepTwo(block, onJobUnitCreated))
    //             {
    //                 continue;
    //             }

    //             StepThree(block, onJobUnitCreated);
    //         }

    //         GameManager.I.GameTime.Register(6 * 60 * 60, () =>
    //         {
    //             foreach (var block in house.Blocks)
    //             {
    //                 var jobUnit = new JobUnit(new WaterPlantAction(new Vector3(block.x, block.y, 0)), onJobUnitFailed: jobUnit =>
    //                 {
    //                     jobUnit.Refresh();
    //                     onJobUnitCreated?.Invoke(jobUnit);
    //                 });
    //                 onJobUnitCreated?.Invoke(jobUnit);
    //             }
    //         });
    //     }

    //     private bool StepOne(Vector3 block, UnityAction<JobUnit> onJobUnitCreated)
    //     {
    //         MapManager.I.TryGetBuildingItem(new Vector3(block.x, block.y, 0), out var buildingItem);
    //         if (buildingItem == null || buildingItem.House.HouseType != HouseType.Farm)
    //         {
    //             var jobUnit = new JobUnit(new HoeAction(new Vector3(block.x, block.y, 0), House), jobUnit =>
    //             {
    //                 StepTwo(block, onJobUnitCreated);
    //             });
    //             onJobUnitCreated?.Invoke(jobUnit);

    //             return true;
    //         }

    //         return false;
    //     }

    //     private bool StepTwo(Vector3 block, UnityAction<JobUnit> onJobUnitCreated)
    //     {
    //         var items = GameManager.I.GameItemManager.GetItemsAtPos(block);
    //         if (items.Count == 0)
    //         {
    //             if (MapManager.I.TryGetBuildingItem(block, out var item))
    //             {
    //                 if (item is FarmItem farmItem)
    //                 {
    //                     var jobUnit = new JobUnit(new PlantAction(farmItem, "PLANT_WHEAT"), jobUnit =>
    //                     {
    //                         StepThree(farmItem.Pos, onJobUnitCreated);
    //                     });
    //                     onJobUnitCreated?.Invoke(jobUnit);
    //                 }
    //             }
    //             return true;
    //         }

    //         return false;
    //     }

    //     private void StepThree(Vector3 block, UnityAction<JobUnit> onJobUnitCreated)
    //     {
    //         var items = GameManager.I.GameItemManager.GetItemsAtPos(block);
    //         foreach (var item in items)
    //         {
    //             if (item is PlantItem plantItem)
    //             {
    //                 plantItem.OnEventInvoked += plantItem =>
    //                 {
    //                     // var jobUnit = new JobUnit(new HarvestAction(plantItem));
    //                     // onJobUnitCreated?.Invoke(jobUnit);
    //                 };
    //                 _plantItems.Add(plantItem);
    //             }
    //         }
    //     }
    // }

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

        public Property Property { get; set; }
        public Queue<JobUnit> JobUnits { get; set; } = new Queue<JobUnit>();

        public JobUnit CheckJobUnit()
        {
            if (JobUnits.Count == 0)
            {
                return null;
            }

            return JobUnits.Dequeue();
        }

        public void AssignJobUnit(JobUnit jobUnit)
        {
            jobUnit.OnJobUnitDone += OnJobUnitDone;
            Property.JobUnits.Add(jobUnit);
            JobUnits.Enqueue(jobUnit);
        }
        
        private void OnJobUnitDone(JobUnit jobUnit)
        {
            jobUnit.OnJobUnitDone -= OnJobUnitDone;
            Property.JobUnits.Remove(jobUnit);
        }
    }

    public class Owner : Job
    {

    }

    public class Employee : Job
    {
        public int WorkDays { get; set; } = 5;
        public int Salary { get; set; } = 1000;

        public Employee(Agent agent)
        {
            Member = agent.Citizen;
        }
    }

    public class Farmer : Employee
    {
        public Farmer(Agent agent) : base(agent)
        {
        }
    }

    public class Cooker : Employee
    {
        public Cooker(Agent agent) : base(agent)
        {
        }
    }

    public class Waiter : Employee
    {
        public Waiter(Agent agent) : base(agent)
        {
        }
    }
}