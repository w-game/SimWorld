using UI;
using UnityEngine;

public class PopBuildingCraft : ViewBase
{
    [SerializeField] private GameObject buildingElemebtPrefab;
    [SerializeField] private Transform buildingElementParent;
    [SerializeField] private GameObject panel;

    public static PopBuildingCraft I { get; private set; }

    void Start()
    {
        var configList = GameManager.I.ConfigReader.GetAllConfigs<BuildingConfig>();
        foreach (var config in configList)
        {
            GenerateBuildingElement(config);
        }
        I = this;
    }

    public static void StartCraft()
    {
        I.panel.SetActive(true);
    }

    private void GenerateBuildingElement(BuildingConfig config)
    {
        var buildingElement = Instantiate(buildingElemebtPrefab, buildingElementParent);
        var buildingElementComponent = buildingElement.GetComponent<BuildingElement>();
        if (buildingElementComponent != null)
        {
            buildingElementComponent.Init(config);
        }
    }

    public void EndCraft()
    {
        BuildingManager.I.CraftMode = false;
        panel.SetActive(false);
    }
}