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
        var paths = config.icon.Split(',');
        var sprites = Resources.LoadAll<Sprite>(paths[0]);

        foreach (var sprite in sprites)
        {
            if (sprite.name == paths[1])
            {
                icon.sprite = sprite;
                break;
            }
        }
    }

    void Start()
    {
        selectBtn.onClick.AddListener(OnSelectBtnClick);
    }

    private void OnSelectBtnClick()
    {
        // Handle the button click event here
        Debug.Log("Building Element selected");
        BuildingManager.I.CraftBuilding(_config);
        BuildingManager.I.CraftMode = true;
    }
}