using UnityEngine;

public class GameTime
{
    public float TimeScale { get; set; } = 72f;
    public float CurrentTime { get; private set; } = 0f;
    public float DeltaTime { get; private set; } = 0f;

    public void Update()
    {
        DeltaTime = Time.deltaTime * TimeScale;
        CurrentTime += DeltaTime;

        if (CurrentTime >= 24f * 60f * 60f)
        {
            // 一天的时间到了，重置时间
            CurrentTime = 0f;
        }
    }
}