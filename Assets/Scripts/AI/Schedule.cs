using System.Collections.Generic;
using Citizens;
using UnityEngine.Events;

namespace AI
{
    public enum SchedulePriority
    {
        Low,
        Medium,
        High
    }

    public class Schedule
    {
        public float StartTime { get; private set; }
        public float EndTime { get; private set; }
        public List<int> Days { get; private set; }
        public SchedulePriority Priority { get; private set; }
        public ActionBase Action { get; private set; }
        public FamilyMember TargetMember { get; private set; }
        private float _elapsedTime;
        private readonly float _duration;

        public Schedule(float startTime, float duration, List<int> days, ActionBase action, FamilyMember targetMember, SchedulePriority priority = SchedulePriority.Medium)
        {
            StartTime = startTime;
            EndTime = startTime + duration;
            Days = days;
            Action = action;
            TargetMember = targetMember;
            Priority = priority;
            _duration = duration;
        }

        public void Update(UnityAction callback)
        {
            _elapsedTime += GameTime.DeltaTime;
            if (_elapsedTime >= _duration)
            {
                TargetMember.Agent.Brain.RegisterAction(Action, false);
                callback?.Invoke();
            }
        }

        public bool Check(float currentTime, int day)
        {
            if (Days.Contains(day) && currentTime >= StartTime)
            {
                TargetMember.Agent.Brain.RegisterAction(Action, true);
                _elapsedTime = 0f;            // reset for Update()
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}