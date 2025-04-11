using System;
using Unity.Cinemachine;
using UnityEngine;

public class UIManager : MonoSingleton<UIManager>
{
    public Camera mainCamera;
    public CinemachineCamera cinemachineCamera;

    internal Vector3 MousePosToWorldPos()
    {
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0; // 设置一个适当的z轴值
        return mousePosition;
    }

    internal Vector3 WorldPosToScreenPos(Vector3 position)
    {
        return mainCamera.WorldToScreenPoint(position);
    }
}