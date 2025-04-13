using System;
using System.Collections.Generic;
using Citizens;
using GameItem;
using UnityEngine;

public class GameItemManager
{
    public List<IGameItem> GameItems { get; private set; } = new List<IGameItem>();
    private Dictionary<Vector2Int, List<IGameItem>> _gameItems = new Dictionary<Vector2Int, List<IGameItem>>();

    public static event Func<Vector3, Vector2Int> ItemPosToMapPosConverter;

    public GameItemManager(Func<Vector3, Vector2Int> func)
    {
        ItemPosToMapPosConverter = func;
    }

    public void RegisterGameItem(IGameItem gameItem)
    {
        var mapPos = ItemPosToMapPosConverter.Invoke(gameItem.Pos);
        if (!_gameItems.ContainsKey(mapPos))
        {
            _gameItems.Add(mapPos, new List<IGameItem>() { gameItem });
        }
        else
        {
            _gameItems[mapPos].Add(gameItem);
        }
        GameItems.Add(gameItem);
    }

    public void UnregisterGameItem(IGameItem gameItem)
    {
        var mapPos = ItemPosToMapPosConverter.Invoke(gameItem.Pos);
        if (_gameItems.ContainsKey(mapPos))
        {
            _gameItems[mapPos].Remove(gameItem);
            if (_gameItems[mapPos].Count == 0)
            {
                _gameItems.Remove(mapPos);
            }
        }
        GameItems.Remove(gameItem);
    }

    public void RemoveGameItemOnMap(IGameItem gameItem)
    {
        var mapPos = ItemPosToMapPosConverter.Invoke(gameItem.Pos);
        if (_gameItems.ContainsKey(mapPos))
        {
            if (_gameItems[mapPos].Contains(gameItem))
            {
                _gameItems[mapPos].Remove(gameItem);
                if (_gameItems[mapPos].Count == 0)
                {
                    _gameItems.Remove(mapPos);
                }
            }
        }
    }

    public List<IGameItem> GetItemsAtPos(Vector3 pos)
    {
        var mapPos = ItemPosToMapPosConverter.Invoke(pos);
        if (_gameItems.ContainsKey(mapPos))
        {
            return _gameItems[mapPos];
        }
        return new List<IGameItem>();
    }

    public void Update()
    {
        foreach (var gameItem in new List<IGameItem>(GameItems))
        {
            gameItem.DoUpdate();
        }
    }

    internal Agent CreateNPC(Vector2Int pos, FamilyMember member)
    {
        var agent = new Agent(null, GameManager.I.ActionSystem.CreateAIController(), pos + new Vector2(0.5f, 0.5f));
        agent.Init(member);
        agent.ShowUI();

        return agent;
    }
}