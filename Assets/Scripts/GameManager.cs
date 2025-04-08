using System;
using System.Collections.Generic;
using Citizens;
using GameItem;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }
    public ConfigReader ConfigReader { get; private set; }
    public ActionSystem ActionSystem { get; private set; }

    public Agent CurrentAgent { get; private set; }
    [SerializeField] private Agent player;
    public GameObject selectSign;

    void Awake()
    {
        I = this;
        ConfigReader = new ConfigReader();
        ConfigReader.LoadConfigs();
        ActionSystem = new ActionSystem();
        ActionSystem.Init();

        CurrentAgent = player;
    }

    // Update is called once per frame
    void Update()
    {
        ActionSystem.Update();
    }

    internal GameObject InstantiateObject(string prefabPath, Vector2 pos, Transform parent = null)
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
