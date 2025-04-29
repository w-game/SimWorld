using UnityEngine;

// Attach to any 2D 物体（要求它有一个 PolygonCollider2D 或 CompositeCollider2D）

[RequireComponent(typeof(SpriteRenderer))]
public class ItemShadow : MonoBehaviour
{
    [Tooltip("阴影方向(世界坐标 2D 单位向量)")]
    public Vector2 shadowDir = new Vector2(1, -1);
    [Tooltip("阴影长度(相对于 Sprite 高度的倍数)")]
    public float shadowLength = 1.5f;
    [Tooltip("影子半宽系数(相对于精灵宽度)")]
    public float beamWidthFactor = 0.25f;
    [Tooltip("根轴心(本地坐标)，通常是 Sprite 底部中心")]
    public Vector2 rootLocal = new Vector2(0.5f, 0.0f);

    SpriteRenderer _sr;
    Material _mat;
    Vector2 _spriteSize;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        // 复制一份实例材质，避免修改到共享资源
        _mat = Instantiate(_sr.sharedMaterial);
        _mat.shader = Shader.Find("Shader Graphs/Demo");
        _sr.material = _mat;

        // 记录 Sprite 大小（本地坐标系）
        var bounds = _sr.sprite.bounds;
        _spriteSize = bounds.size;
    }

    void LateUpdate()
    {
        // Sprite.bounds is in world space; we need local space.
        // Since SpriteRenderer vertices are generated in local space with pivot at (0,0),
        // the local extents are ±_sr.sprite.bounds.extents.
        Vector2 ext = _sr.sprite.bounds.extents;  // half‑size in local units
        // Interpolate between –ext (0) and +ext (1) using rootLocal (0‑1 range)
        Vector2 root = new Vector2(
            Mathf.Lerp(-ext.x, ext.x, rootLocal.x),
            Mathf.Lerp(-ext.y, ext.y, rootLocal.y)
        );

        float length = _sr.sprite.bounds.size.y * shadowLength;
        float width  = _sr.sprite.bounds.size.x * beamWidthFactor;

        // 设置材质参数
        _mat.SetVector("_Root", root);
        _mat.SetVector("_Dir", shadowDir.normalized);
        _mat.SetFloat("_Length", length);
        _mat.SetFloat("_Width", width);
        _mat.SetColor("_Color", new Color(0, 0, 0, 0.5f));
    }
}