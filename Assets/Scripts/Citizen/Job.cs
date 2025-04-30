using System;
using System.Collections.Generic;
using AI;
using UnityEngine;
using UnityEngine.Events;

namespace Citizens
{
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
            Action.OnCompleted += (action, success) =>
            {
                if (!success)
                {
                    Done = false;
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

    public abstract class Job
    {
        public FamilyMember Member { get; set; }
        public float[] WorkTime { get; set; } = new float[2] { 0 * 60 * 60, 18 * 60 * 60 };

        public Property Property { get; set; }
        public JobUnit CurJob { get; private set; }
        public bool AutoAssign { get; set; } = true;

        public event Action OnJobUnitAssigned;

        public Job(FamilyMember member)
        {
            Member = member;
        }

        public JobUnit CheckJobUnit()
        {
            if (Property.JobUnits.Count == 0)
            {
                return null;
            }

            foreach (var jobUnit in Property.JobUnits)
            {
                if (jobUnit.Key == GetType())
                {
                    if (jobUnit.Value.Count > 0)
                    {
                        jobUnit.Value.RemoveAt(0);
                        return jobUnit.Value[0];
                    }
                }
            }

            if (this is Owner)
            {
                foreach (var jobUnit in Property.JobUnits)
                {
                    if (jobUnit.Value.Count > 0)
                    {
                        jobUnit.Value.RemoveAt(0);
                        return jobUnit.Value[0];
                    }
                }
            }

            return null;
        }

        private void OnJobUnitDone(JobUnit jobUnit)
        {
            CurJob = null;
            jobUnit.OnJobUnitDone -= OnJobUnitDone;
        }

        public void Next()
        {
            if (AutoAssign)
            {
                CurJob = CheckJobUnit();
                if (CurJob != null)
                {
                    CurJob.OnJobUnitDone += OnJobUnitDone;
                    OnJobUnitAssigned?.Invoke();
                }
            }
        }

        public void DoJobUnit(Type key, JobUnit jobUnit)
        {
            CurJob = jobUnit;
            Property.JobUnits[key].Remove(jobUnit);
            CurJob.OnJobUnitDone += OnJobUnitDone;
            OnJobUnitAssigned?.Invoke();
        }
    }

    public class Owner : Job
    {
        public Owner(FamilyMember member) : base(member)
        {
        }
    }

    public class Employee : Job
    {
        public int WorkDays { get; set; } = 5;
        public int Salary { get; set; } = 1000;

        public Employee(FamilyMember member) : base(member)
        {
        }
    }

    public class Farmer : Employee
    {
        public Farmer(FamilyMember member) : base(member)
        {
        }
    }

    public class Cooker : Employee
    {
        public Cooker(FamilyMember member) : base(member)
        {
        }
    }

    public class Waiter : Employee
    {
        public Waiter(FamilyMember member) : base(member)
        {
        }
    }
    
    public class Salesman : Employee
    {
        public Salesman(FamilyMember member) : base(member)
        {
        }
    }
}