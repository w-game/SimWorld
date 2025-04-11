using UnityEngine;

public class GameTime
{
    public float TimeScale { get; set; } = 1f;
    public float CurrentTime { get; private set; } = 0f;


    public void Update()
    {
        CurrentTime += Time.deltaTime * TimeScale;
    }
}