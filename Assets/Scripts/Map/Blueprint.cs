using UnityEngine;

public class Blueprint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    public void Place(Vector2Int position, Sprite sprite)
    {
        // 将蓝图放置在指定位置
        transform.position = new Vector3(position.x, position.y, 0);
        sr.sprite = sprite;
    }
}