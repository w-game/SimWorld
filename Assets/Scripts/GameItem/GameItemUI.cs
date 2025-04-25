using System;
using GameItem;
using UnityEngine;

public class GameItemUI : MonoBehaviour, IPoolable
{
    [SerializeField] private SpriteRenderer sr;
    public IGameItem GameItem { get; private set; }

    public virtual void Init(IGameItem gameItem)
    {
        GameItem = gameItem;
    }

    internal void SetRenderer(string spritePath, float alpha = 1)
    {
        sr.sprite = Resources.Load<Sprite>(spritePath);
        sr.color = new Color(1, 1, 1, alpha);
    }
    
    public virtual void OnGet()
    {

    }

    public virtual void OnRelease()
    {
        
    }
}