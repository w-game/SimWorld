using UnityEngine;
using UnityEngine.UI;

public class BuildingElement : MonoBehaviour
{
    [SerializeField] private Button selectBtn;
    [SerializeField] private Image icon;

    private BuildingConfig _config;

    public void Init(BuildingConfig config)
    {
        _config = config;
        icon.sprite = Resources.Load<Sprite>(_config.icon);
    }

    void Start()
    {
        selectBtn.onClick.AddListener(OnSelectBtnClick);
    }

    private void OnSelectBtnClick()
    {
        // Handle the button click event here
        Debug.Log("Building Element selected");
        BuildingManager.I.StartBuildingMode(_config);
    }
}