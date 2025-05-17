using System;
using System.Collections.Generic;
using AI;
using UI.Elements;
using UnityEngine.Events;

namespace Citizens
{
    public enum JobUnitType
    {
        Farm,
        Service,
        Cook,
        Sale,
        Manage
    }

    public enum WorkType
    {
        Farmer,
        FarmHelper,
        Waiter,
        Cooker,
        Salesman,
        CEO
    }

    public class JobUnit
    {
        public JobUnitType Type { get; private set; }
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

        public JobUnit(JobUnitType unitType, ActionBase action, UnityAction<JobUnit> onJobUnitDone = null, UnityAction<JobUnit> onJobUnitFailed = null)
        {
            Type = unitType;
            OnJobUnitDone += onJobUnitDone;
            Action = action;
            Action.OnCompleted += (action, success) =>
            {
                if (success)
                {
                    Done = true;
                }
                else
                {
                    // failure case
                    onJobUnitFailed?.Invoke(this);
                }
            };
        }

        public void Refresh()
        {
            Done = false;
            Action.Reset();
        }
    }

    public abstract class Work
    {
        public FamilyMember Member { get; set; }
        public float[] WorkTime { get; set; } = new float[2] { 0 * 60 * 60, 18 * 60 * 60 };

        public BusinessProperty Property { get; set; }
        public JobUnit CurJob { get; private set; }
        public bool AutoAssign { get; set; } = true;

        public event Action OnJobUnitAssigned;

        public abstract List<JobUnitType> ExpectJobUnits { get; }

        public Work(FamilyMember member, BusinessProperty property)
        {
            Member = member;
            Property = property;
            AutoAssign = member.Agent == GameManager.I.CurrentAgent ? false : true;
        }

        public virtual JobUnit CheckJobUnit()
        {
            if (Property == null || Property.JobBoard.Count == 0)
            {
                return null;
            }

            return Property.JobBoard.RequestJob(ExpectJobUnits);
        }

        private void OnJobUnitDone(JobUnit jobUnit)
        {
            CurJob = null;
            jobUnit.OnJobUnitDone -= OnJobUnitDone;
        }

        public void Next()
        {
            CurJob = CheckJobUnit();
            if (CurJob != null)
            {
                CurJob.OnJobUnitDone += OnJobUnitDone;
                OnJobUnitAssigned?.Invoke();
            }
        }

        public bool DoJobUnit(JobUnitType key, JobUnit jobUnit)
        {
            if (CurJob != null) {
                MessageBox.I.ShowMessage("You are already doing a job.", "Textures/Error", MessageType.Error);
                return false;
            }
            CurJob = jobUnit;

            Queue<JobUnit> newJobUnits = new Queue<JobUnit>();

            while (Property.JobBoard.Count > 0)
            {
                var unit = Property.JobBoard.RequestJob(new List<JobUnitType> { key });
                if (unit == jobUnit) continue;
                newJobUnits.Enqueue(unit);
            }
            Property.JobBoard.Cover(key, newJobUnits);
            CurJob.OnJobUnitDone += OnJobUnitDone;
            OnJobUnitAssigned?.Invoke();
            Member.Agent.Brain.RegisterAction(CurJob.Action, true);
            return true;
        }

        public void Resign()
        {
            
        }
    }

    public class CEO : Work
    {
        public override List<JobUnitType> ExpectJobUnits => new List<JobUnitType> { JobUnitType.Manage };

        public CEO(FamilyMember member, BusinessProperty property) : base(member, property)
        {
        }

        public void RemoveProperty(BusinessProperty property)
        {
            if (Property == property)
            {
                Property = null;
            }
        }
    }

    public class AssetAgent : Work
    {
        public override List<JobUnitType> ExpectJobUnits => new List<JobUnitType> { JobUnitType.Manage };

        public AssetAgent(FamilyMember owner, BusinessProperty property) : base(owner, property)
        {
            Property = property;
        }
    }

    public abstract class Employee : Work
    {
        public static readonly float[] DefaultWorkTime = new float[2] { 0 * 60 * 60, 18 * 60 * 60 };
        public int WorkDays { get; set; } = 5;

        public Employee(FamilyMember member, BusinessProperty property, float[] workTime = null) : base(member, property)
        {
            if (workTime == null)
            {
                workTime = DefaultWorkTime;
            }
            Schedule schedule = new Schedule(
                workTime[0], workTime[1],
                new List<int> { 1, 2, 3, 4, 5, 6, 7 },
                ActionPool.Get<WorkAction>(this), member
            );
            member.Agent.RegisterSchedule(schedule, "WorkSchedule");
        }
    }

    public class Farmer : CEO
    {
        public override List<JobUnitType> ExpectJobUnits => new List<JobUnitType> { JobUnitType.Farm };
        
        private List<FarmProperty> _farms;

        public Farmer(FamilyMember member, List<FarmProperty> farms) : base(member, farms[0])
        {
            _farms = farms;
        }

        public override JobUnit CheckJobUnit()
        {
            var jobUnit = base.CheckJobUnit();
            if (jobUnit == null)
            {
                Property = _farms.Find(x => x.JobBoard.Count > 0);
                jobUnit = base.CheckJobUnit();
            }
            return jobUnit;
        }
    }

    public class FarmHelper : Employee
    {
        public override List<JobUnitType> ExpectJobUnits => new List<JobUnitType> { JobUnitType.Farm };

        public FarmHelper(FamilyMember member, BusinessProperty property, float[] workTime = null) : base(member, property, workTime)
        {
        }
    }

    public class Cooker : Employee
    {
        public override List<JobUnitType> ExpectJobUnits => new List<JobUnitType> { JobUnitType.Cook };
        public Cooker(FamilyMember member, BusinessProperty property, float[] workTime = null) : base(member, property, workTime)
        {
        }
    }

    public class Waiter : Employee
    {
        public override List<JobUnitType> ExpectJobUnits => new List<JobUnitType> { JobUnitType.Service };

        public Waiter(FamilyMember member, BusinessProperty property, float[] workTime = null) : base(member, property, workTime)
        {
        }
    }

    public class Salesman : Employee
    {
        public override List<JobUnitType> ExpectJobUnits => new List<JobUnitType> { JobUnitType.Sale };

        public Salesman(FamilyMember member, BusinessProperty property, float[] workTime = null) : base(member, property, workTime)
        {
        }
    }
}