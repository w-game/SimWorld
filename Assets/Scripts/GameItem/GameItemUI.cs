using System;
using System.Collections.Generic;
using GameItem;
using TMPro;
using UnityEngine;

public class GameItemUI : MonoBehaviour, IPoolable
{
    [SerializeField] private TextMeshProUGUI itemNameText;

    private PolygonCollider2D _col;
    public Collider2D Col
    {
        get
        {
            if (_col == null)
            {
                _col = gameObject.AddComponent<PolygonCollider2D>();
                _col.isTrigger = true;
            }
            return _col;
        }
    }
    public IGameItem GameItem { get; private set; }

    private IGameItem _animationTarget;
    private Action _animationCallback;
    private SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public virtual void Init(IGameItem gameItem)
    {
        GameItem = gameItem;
    }

    internal void SetRenderer(string spritePath, float alpha = 1)
    {
        _sr.sprite = Resources.Load<Sprite>(spritePath);
        _sr.color = new Color(1, 1, 1, alpha);

        if (_col == null)
        {
            _col = gameObject.AddComponent<PolygonCollider2D>();
            _col.isTrigger = true;
        }
        else
        {
            ResetPolygonCollider();
        }
    }

    void ResetPolygonCollider()
    {
        var sprite = _sr.sprite;
        if (sprite == null || _col == null) return;

        _col.pathCount = sprite.GetPhysicsShapeCount();
        for (int i = 0; i < _col.pathCount; i++)
        {
            var shape = new List<Vector2>();
            sprite.GetPhysicsShape(i, shape);
            _col.SetPath(i, shape.ToArray());
        }
    }

    public void SetName(string name)
    {
        if (itemNameText == null)
            return;
        itemNameText.text = name;
    }

    public virtual void OnGet()
    {
        if (_col != null)
            _col.enabled = true;

    }

    public virtual void OnRelease()
    {
        _animationTarget = null;
        _animationCallback = null;
    }

    public void PlayAnimation(IGameItem target, Action value)
    {
        _animationTarget = target;
        _animationCallback = value;
    }

    void FixedUpdate()
    {
        if (_animationTarget != null)
        {
            transform.position = Vector3.Lerp(transform.position, _animationTarget.Pos, Time.deltaTime * 20);
            if (Vector3.Distance(transform.position, _animationTarget.Pos) < 0.2f)
            {
                _animationCallback?.Invoke();
            }
        }
    }
}