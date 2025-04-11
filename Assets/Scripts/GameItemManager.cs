using System.Collections.Generic;
using GameItem;
using UnityEngine;

public class GameItemManager
{
    public List<IGameItem> GameItems { get; private set; } = new List<IGameItem>();
    public void RegisterGameItem(IGameItem gameItem)
    {
        GameItems.Add(gameItem);
    }

    public void UnregisterGameItem(IGameItem gameItem)
    {
        GameItems.Remove(gameItem);
        Object.Destroy(gameItem.UI.gameObject);
    }

    public void Update()
    {
        foreach (var gameItem in GameItems)
        {
            gameItem.DoUpdate();
        }
    }
}