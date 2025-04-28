using UnityEngine;
using UnityEngine.Rendering.Universal;

// Attach to any 2D 物体（要求它有一个 PolygonCollider2D 或 CompositeCollider2D）

[RequireComponent(typeof(SpriteRenderer))]
public class ItemShadow : MonoBehaviour
{
    [Tooltip("阴影方向(世界坐标 2D 单位向量)")]
    public Vector2 shadowDir = new Vector2(1, -1);
    [Tooltip("阴影长度(相对于 Sprite 高度的倍数)")]
    public float shadowLength = 1.5f;
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
        _mat.shader = Shader.Find("Custom/DirectionalShadow");
        _sr.material = _mat;

        // 记录 Sprite 大小（本地坐标系）
        var bounds = _sr.sprite.bounds;
        _spriteSize = bounds.size;
    }

    void LateUpdate()
    {
        // local‑space bottom‑left corner of the sprite mesh
        Vector2 min = _sr.sprite.bounds.min;
        // compute root point by interpolating inside the sprite bounds
        Vector2 root = min + Vector2.Scale(_sr.sprite.bounds.size, rootLocal);

        // 2. 计算缩放因子 k = (height + shadowLength) / height
        float k = (1 + shadowLength);

        // 3. 设置材质参数
        _mat.SetVector("_Root", root);
        _mat.SetVector("_Dir", shadowDir.normalized);
        _mat.SetFloat("_Scale", k);
        _mat.SetColor("_Color", new Color(0, 0, 0, 0.5f));
    }
}