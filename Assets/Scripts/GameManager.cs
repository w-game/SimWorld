using AI;
using Citizens;
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
    }

    private void CreatePlayer()
    {
        CurrentAgent = new Agent(null, ActionSystem.CreateAIController(), Vector2.zero);
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
}
