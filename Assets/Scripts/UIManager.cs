using System;
using UI.Models;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public interface IUIPoolable : IPoolable
{
}

public class UIManager : MonoSingleton<UIManager>
{
    public Camera mainCamera;
    public CinemachineCamera cinemachineCamera;

    public bool IsClickUI => EventSystem.current.IsPointerOverGameObject();

    public static event UnityAction<Vector3> OnMouseBtnClicked;
    private ObjectPool UIPool { get; } = new ObjectPool(30);

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


    public Vector3 ScreenPosToWorldPos(Vector3 position)
    {
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(position);
        worldPosition.z = 0; // 设置一个适当的z轴值
        return worldPosition;
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
            var model = IModel.GetModel<PopBagModel>();
            model.ShowUI();
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            var model = IModel.GetModel<PopJobUnitsModel>();
            model.ShowUI(GameManager.I.CurrentAgent.Citizen.Work);
        }
    }

    public T GetElement<T>(string prefabPath, Vector3 pos, Transform parent) where T : MonoBehaviour, IUIPoolable
    {
        return UIPool.Get<T>(prefabPath, pos, parent);
    }

    internal void ReleaseElement<T>(T instance, string prefabPath) where T : MonoBehaviour, IUIPoolable
    {
        UIPool.Release(instance, prefabPath);
    }
}