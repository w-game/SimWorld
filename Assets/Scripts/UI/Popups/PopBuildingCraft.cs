using UI;
using UnityEngine;

public class PopBuildingCraft : ViewBase
{
    [SerializeField] private GameObject buildingElemebtPrefab;
    [SerializeField] private Transform buildingElementParent;

    void Start()
    {
        var configList = ConfigReader.GetAllConfigs<BuildingConfig>();
        foreach (var config in configList)
        {
            GenerateBuildingElement(config);
        }
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

    public override void OnHide()
    {
        base.OnHide();
        BuildingManager.I.StopBuildingMode();
    }
}