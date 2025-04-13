using System;
using GameItem;
using UnityEngine;

public class GameItemUI : MonoBehaviour, IPoolable
{
    [SerializeField] private SpriteRenderer sr;
    public GameItemBase GameItem { get; private set; }

    public virtual void Init(GameItemBase gameItem)
    {
        GameItem = gameItem;
    }

    internal void SetRenderer(string spritePath)
    {
        sr.sprite = Resources.Load<Sprite>(spritePath);
    }

    public virtual void OnGet()
    {
        
    }

    public virtual void OnRelease()
    {
        
    }
}