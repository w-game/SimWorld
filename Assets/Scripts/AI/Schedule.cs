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

        public Schedule(float startTime, float endTime, List<int> days, ActionBase action, FamilyMember targetMember, SchedulePriority priority = SchedulePriority.Medium)
        {
            StartTime = startTime;
            EndTime = endTime;
            Days = days;
            Action = action;
            TargetMember = targetMember;
            Priority = priority;
            _duration = EndTime >= StartTime ? EndTime - StartTime
                                             : (24f - StartTime) + EndTime; // wrap‑around
        }

        public void Update(UnityAction callback)
        {
            _elapsedTime += GameManager.I.GameTime.DeltaTime;
            if (_elapsedTime >= _duration)
            {
                TargetMember.Agent.Brain.RegisterAction(Action, false);
                callback?.Invoke();
            }
        }

        public bool Check(float currentTime, int day)
        {
            // Determine which schedule “day” this time belongs to.
            // If the window spans midnight and we are after 00:00 but before EndTime,
            // we should use the *previous* day index.
            int effectiveDay = day;
            bool timeInWindow;

            if (StartTime <= EndTime)
            {
                timeInWindow = currentTime >= StartTime && currentTime <= EndTime;
            }
            else
            {
                // Cross‑midnight window, e.g. 22:00‑02:00
                timeInWindow = currentTime >= StartTime || currentTime <= EndTime;
                if (currentTime <= EndTime)           // after midnight – belongs to previous day
                    effectiveDay = (day + 7) % 7; // wrap to 0‑6
            }

            if (!timeInWindow || !Days.Contains(effectiveDay))
                return false;

            TargetMember.Agent.Brain.RegisterAction(Action, true);
            _elapsedTime = 0f;            // reset for Update()
            return true;
        }
    }
}