using System.Collections.Generic;
using Citizens;

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

        public Schedule(float startTime, float endTime, List<int> days, ActionBase action, FamilyMember targetMember, SchedulePriority priority = SchedulePriority.Medium)
        {
            StartTime = startTime;
            EndTime = endTime;
            Days = days;
            Action = action;
            TargetMember = targetMember;
            Priority = priority;
        }

        public bool Check(float currentTime, int day)
        {
            if (!Days.Contains(day)) return false;
            if (currentTime >= StartTime && currentTime <= EndTime)
            {
                TargetMember.Agent.Brain.RegisterAction(Action, true);
                return true;
            }

            return false;
        }
    }
}