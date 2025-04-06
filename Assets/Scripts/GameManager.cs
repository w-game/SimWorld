using System;
using System.Collections.Generic;
using Citizens;
using GameItem;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }
    public ActionSystem ActionSystem { get; private set; }

    public Agent CurrentAgent { get; private set; }
    [SerializeField] private Agent player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        I = this;
        ActionSystem = new ActionSystem();
        ActionSystem.Init();

        CurrentAgent = player;
    }

    // Update is called once per frame
    void Update()
    {
        ActionSystem.Update();
    }

    internal GameObject InstantiateObject(string prefabPath, Vector3 pos, Transform parent = null)
    {
        var prefab = Resources.Load<GameObject>(prefabPath);
        var obj = Instantiate(prefab, pos, Quaternion.identity);
        if (parent != null)
        {
            obj.transform.SetParent(parent);
        }

        return obj;
    }
}
