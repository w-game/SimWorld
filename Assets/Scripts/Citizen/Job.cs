using System;
using System.Collections.Generic;
using AI;
using UI.Elements;
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
            AutoAssign = member.Agent == GameManager.I.CurrentAgent ? false : true;
        }

        public virtual JobUnit CheckJobUnit()
        {
            if (Property != null && Property.JobUnits.Count == 0)
            {
                return null;
            }

            foreach (var jobUnit in Property.JobUnits)
            {
                if (jobUnit.Key == GetType())
                {
                    if (jobUnit.Value.Count > 0)
                    {
                        var unit = jobUnit.Value[0];
                        jobUnit.Value.RemoveAt(0);
                        return unit;
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
            CurJob = CheckJobUnit();
            if (CurJob != null)
            {
                CurJob.OnJobUnitDone += OnJobUnitDone;
                OnJobUnitAssigned?.Invoke();
            }
        }

        public bool DoJobUnit(Type key, JobUnit jobUnit)
        {
            if (CurJob != null) {
                MessageBox.I.ShowMessage("You are already doing a job.", "Textures/Error", MessageType.Error);
                return false;
            }
            CurJob = jobUnit;
            Property.JobUnits[key].Remove(jobUnit);
            CurJob.OnJobUnitDone += OnJobUnitDone;
            OnJobUnitAssigned?.Invoke();
            Member.Agent.Brain.RegisterAction(CurJob.Action, true);
            return true;
        }

        public void Resign()
        {
            
        }
    }

    public class Owner : Job
    {
        public List<Property> Properties { get; private set; } = new List<Property>();
        public Owner(FamilyMember member) : base(member)
        {
        }

        public void AddProperty(Property property)
        {
            Properties.Add(property);
            ChangeProperty();
        }

        public void RemoveProperty(Property property)
        {
            Properties.Remove(property);
            if (Property == property)
            {
                ChangeProperty();
            }
        }

        private void ChangeProperty()
        {
            var property = Properties.Find(p => p.Rentant == null && p.JobUnits.ContainsKey(GetType()) && p.JobUnits[GetType()].Count > 0);
            if (property != null)
            {
                Property = property;
            }
            else
            {
                property = Properties.Find(p => p.JobUnits.Count > 0);
                if (property != null)
                {
                    Property = property;
                }
            }

            if (Property == null && Properties.Count > 0)
            {
                Property = Properties[0];
            }
        }

        public override JobUnit CheckJobUnit()
        {
            if (Property == null || Property.JobUnits.Count == 0)
            {
                ChangeProperty();
                if (Property == null) return null;
            }

            if (Property.Rentant != null)
            {
                Property = null;
                return null;
            }

            Property.JobUnits.TryGetValue(GetType(), out var jobUnits);

            foreach (var kv in Property.JobUnits)
            {
                if (kv.Value.Count > 0)
                {
                    var unit = kv.Value[0];
                    kv.Value.RemoveAt(0);
                    return unit;
                }
            }
            return null;
        }
    }

    public class Rentant : Job
    {
        public Rentant(FamilyMember member, Property property) : base(member)
        {
            Property = property;
        }

        public override JobUnit CheckJobUnit()
        {
            foreach (var kv in Property.JobUnits)
            {
                if (kv.Value.Count > 0)
                {
                    var unit = kv.Value[0];
                    kv.Value.RemoveAt(0);
                    return unit;
                }
            }
            return null;
        }
    }

    public class Employee : Job
    {
        public int WorkDays { get; set; } = 5;

        public Employee(FamilyMember member, float[] workTime) : base(member)
        {
            Schedule schedule = new Schedule(
                workTime[0], workTime[1],
                new List<int> { 1, 2, 3, 4, 5, 6, 7 },
                ActionPool.Get<WorkAction>(this), member
            );
            member.Agent.RegisterSchedule(schedule, "WorkSchedule");
        }
    }

    public class Farmer : Employee
    {
        public Farmer(FamilyMember member, float[] workTime) : base(member, workTime)
        {
        }
    }

    public class Cooker : Employee
    {
        public Cooker(FamilyMember member, float[] workTime) : base(member, workTime)
        {
        }
    }

    public class Waiter : Employee
    {
        public Waiter(FamilyMember member, float[] workTime) : base(member, workTime)
        {
        }
    }
    
    public class Salesman : Employee
    {
        public Salesman(FamilyMember member, float[] workTime) : base(member, workTime)
        {
        }
    }
}