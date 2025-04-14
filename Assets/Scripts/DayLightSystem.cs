using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class DayLightSystem : MonoSingleton<MonoBehaviour>
{
    [SerializeField] private Light2D globalLight;

    private float currentTime => GameManager.I.GameTime.CurrentTime;

    private void Start()
    {
    }

    private void Update()
    {
        var time = currentTime / 60f / 60f; // 转换为小时
        Color color;
        float intensity;

        if (time >= 5f && time < 7f) // 清晨
        {
            float t = Mathf.InverseLerp(5f, 7f, time);
            color = Color.Lerp(new Color(0.1f, 0.1f, 0.2f), new Color(1f, 0.9f, 0.6f), t);
            intensity = Mathf.Lerp(0.2f, 1f, t);
        }
        else if (time >= 7f && time < 17f) // 白天
        {
            color = Color.white;
            intensity = 1f;
        }
        else if (time >= 17f && time < 19f) // 傍晚
        {
            float t = Mathf.InverseLerp(17f, 19f, time);
            color = Color.Lerp(new Color(1f, 0.9f, 0.6f), new Color(0.3f, 0.2f, 0.5f), t);
            intensity = Mathf.Lerp(1f, 0.3f, t);
        }
        else // 夜晚
        {
            color = new Color(0.1f, 0.1f, 0.2f);
            intensity = 0.2f;
        }

        globalLight.color = color;
        globalLight.intensity = intensity;
    }
}