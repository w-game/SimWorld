using System;
using GameItem;
using UnityEngine;

public class GameItemUI : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    public GameItemBase GameItem { get; private set; }

    public void Init(GameItemBase gameItem)
    {
        GameItem = gameItem;
    }

    internal void SetRenderer(string spritePath)
    {
        sr.sprite = Resources.Load<Sprite>(spritePath);
    }
}