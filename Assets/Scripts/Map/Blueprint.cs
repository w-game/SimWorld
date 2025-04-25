using UnityEngine;

public class Blueprint : MonoBehaviour
{
    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void Place(Sprite sprite)
    {
        _sr.sprite = sprite;
    }
}

public class BuildingSign : Blueprint
{
    void Update()
    {
        var mousePos = UIManager.I.MousePosToWorldPos();
        var gridPos = MapManager.I.WorldPosToCellPos(mousePos);
        transform.position = new Vector2(gridPos.x + 0.5f, gridPos.y + 0.5f);
    }
}