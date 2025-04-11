using AI;
using Citizens;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }
    public ConfigReader ConfigReader { get; private set; }
    public ActionSystem ActionSystem { get; private set; }
    public CitizenManager CitizenManager { get; private set; }

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

        GameTime = new GameTime();

        CreatePlayer();
    }

    // Update is called once per frame
    void Update()
    {
        ActionSystem.Update();
        GameTime.Update();
    }

    private void CreatePlayer()
    {
        var player = InstantiateObject("Prefabs/Player", Vector2.zero);
        player.name = "Player";
        var ciziten = CitizenManager.CreatePlayer();
        CurrentAgent = player.GetComponent<Agent>();
        CurrentAgent.Init(ciziten);
        UIManager.I.cinemachineCamera.Follow = player.transform;
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
