using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimeToInvoke
{
    public float Time { get; private set; }
    public UnityAction Action { get; private set; }

    public TimeToInvoke(float time, UnityAction action)
    {
        Time = time;
        Action = action;
    }
}

public class GameTime
{
    public float TimeScale { get; set; } = 72f;
    public float CurrentTime { get; private set; } = 0f;
    public float DeltaTime { get; private set; } = 0f;
    public int Day { get; private set; } = 1;

    private List<TimeToInvoke> timeToInvokeList = new List<TimeToInvoke>();

    public void Update()
    {
        DeltaTime = Time.deltaTime * TimeScale;
        CurrentTime += DeltaTime;

        if (CurrentTime >= 24f * 60f * 60f)
        {
            // 一天的时间到了，重置时间
            CurrentTime = 0f;
            Day++;
            if (Day > 7)
            {
                Day = 1; // 一周循环
            }
        }

        foreach (var timeToInvoke in timeToInvokeList)
        {
            if (CurrentTime >= timeToInvoke.Time)
            {
                timeToInvoke.Action?.Invoke();
            }
        }
    }

    public void Register(float time, UnityAction action)
    {
        timeToInvokeList.Add(new TimeToInvoke(time, action));
    }
}