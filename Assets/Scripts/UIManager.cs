using UI.Models;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class UIManager : MonoSingleton<UIManager>
{
    public Camera mainCamera;
    public CinemachineCamera cinemachineCamera;

    public static event UnityAction<Vector3> OnMouseBtnClicked;

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

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = MousePosToWorldPos();
            OnMouseBtnClicked?.Invoke(mousePos);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var model = new PopBuildingCraftModel();
            model.ShowUI();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            var model = new PopBagModel();
            model.ShowUI();
        }
    }
}