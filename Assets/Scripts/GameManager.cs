using System.Collections.Generic;
using AI;
using Citizens;
using GameItem;
using UI.Views;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }
    public ConfigReader ConfigReader { get; private set; }
    public ActionSystem ActionSystem { get; private set; }
    public CitizenManager CitizenManager { get; private set; }
    public GameItemManager GameItemManager { get; private set; }

    public Agent CurrentAgent { get; private set; }
    public GameObject selectSign;

    public GameTime GameTime { get; private set; }

    void Awake()
    {
        I = this;
        ConfigReader = new ConfigReader();
        ConfigReader.LoadConfigs();
        ActionSystem = new ActionSystem();
        ActionSystem.Init();
        CitizenManager = new CitizenManager();
        GameItemManager = new GameItemManager(MapManager.I.WorldPosToCellPos);

        GameTime = new GameTime();

        CreatePlayer();
        ActionListElement.I.Init();
    }

    void Update()
    {
        ActionSystem.Update();
        GameTime.Update();
        GameItemManager.Update();

        if (Input.GetKeyDown(KeyCode.E))
        {
            // CurrentAgent.RegisterSchedule(
            //     new Schedule(
            //         20 * 60 * 60,
            //         29 * 60 * 60,
            //         new List<int>() { 1, 2, 3, 4, 5, 6, 7 },
            //         new SleepAction(CurrentAgent.State.Sleep),
            //         CurrentAgent.Citizen));

            CurrentAgent.Bag.AddItem(ConfigReader.GetConfig<PropConfig>("PROP_SEED_WHEAT"), 1);
        }
    }

    private void CreatePlayer()
    {
        CurrentAgent = GameItemManager.CreateGameItem<Agent>(
            null,
            Vector3.zero,
            GameItemType.Dynamic,
            ActionSystem.CreateAIController()
        );
        CurrentAgent.Init(CitizenManager.CreatePlayer());
        CurrentAgent.ShowUI();
        UIManager.I.cinemachineCamera.Follow = CurrentAgent.UI.transform;
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

    public void CraftItem(CraftConfig config)
    {
        foreach (var item in config.materials)
        {
            CurrentAgent.Bag.RemoveItem(ConfigReader.GetConfig<PropConfig>(item.id), item.amount);
        }

        CurrentAgent.Bag.AddItem(ConfigReader.GetConfig<PropConfig>(config.id), 1);
    }
}
