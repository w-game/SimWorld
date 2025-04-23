using System;
using System.Collections.Generic;
using System.Linq;
using Citizens;
using GameItem;
using UnityEngine;

public class GameItemManager
{
    private Dictionary<Vector2Int, List<IGameItem>> _staticGameItems = new Dictionary<Vector2Int, List<IGameItem>>();
    private List<IGameItem> _dynamicGameItems = new List<IGameItem>();

    public static event Func<Vector3, Vector2Int> ItemPosToMapPosConverter;

    public ObjectPool<GameItemUI> ItemUIPool { get; private set; }

    public GameItemManager(Func<Vector3, Vector2Int> func)
    {
        ItemPosToMapPosConverter = func;
        ItemUIPool = new ObjectPool<GameItemUI>(128);
    }

    public void RegisterGameItem(IGameItem gameItem)
    {
        if (gameItem is StaticGameItem staticGameItem)
        {
            var mapPos = ItemPosToMapPosConverter.Invoke(gameItem.Pos);
            if (!_staticGameItems.ContainsKey(mapPos))
            {
                _staticGameItems.Add(mapPos, new List<IGameItem>() { gameItem });
            }
            else
            {
                _staticGameItems[mapPos].Add(gameItem);
            }
        } else if (gameItem is DynamicGameItem)
        {
            _dynamicGameItems.Add(gameItem);
        }
    }

    public void UnregisterGameItem(IGameItem gameItem)
    {
        if (gameItem is DynamicGameItem)
        {
            _dynamicGameItems.Remove(gameItem);
            return;
        }

        var mapPos = ItemPosToMapPosConverter.Invoke(gameItem.Pos);
        if (_staticGameItems.ContainsKey(mapPos))
        {
            _staticGameItems[mapPos].Remove(gameItem);
            if (_staticGameItems[mapPos].Count == 0)
            {
                _staticGameItems.Remove(mapPos);
            }
        }
    }

    public void RemoveGameItemOnMap(IGameItem gameItem)
    {
        if (gameItem is DynamicGameItem)
        {
            return;
        }
        
        var mapPos = ItemPosToMapPosConverter.Invoke(gameItem.Pos);
        if (_staticGameItems.ContainsKey(mapPos))
        {
            if (_staticGameItems[mapPos].Contains(gameItem))
            {
                _staticGameItems[mapPos].Remove(gameItem);
                if (_staticGameItems[mapPos].Count == 0)
                {
                    _staticGameItems.Remove(mapPos);
                }
            }
        }
    }

    public List<IGameItem> GetItemsAtPos(Vector3 pos)
    {
        var mapPos = ItemPosToMapPosConverter.Invoke(pos);
        if (_staticGameItems.ContainsKey(mapPos))
        {
            return _staticGameItems[mapPos];
        }
        return new List<IGameItem>();
    }

    public void Update()
    {
        foreach (var gameItem in new List<IGameItem>(_staticGameItems.Values.SelectMany(x => x)))
        {
            gameItem.DoUpdate();
            var dis = Vector2.Distance(gameItem.Pos, GameManager.I.CurrentAgent.Pos);
            if (dis > 64)
            {
                gameItem.HideUI();
            }
            else if (gameItem.UI == null && gameItem is not BuildingItem)
            {
                gameItem.ShowUI();
            }

            if (dis > 256)
            {
                gameItem.Destroy();
            }
        }

        foreach (var gameItem in new List<IGameItem>(_dynamicGameItems))
        {
            gameItem.DoUpdate();
            var dis = Vector2.Distance(gameItem.Pos, GameManager.I.CurrentAgent.Pos);
            if (dis > 64)
            {
                gameItem.HideUI();
            }
            else if (gameItem.UI == null)
            {
                gameItem.ShowUI();
            }

            if (dis > 256)
            {
                gameItem.Destroy();
            }
        }
    }

    internal Agent CreateNPC(Vector2Int pos, FamilyMember member)
    {
        var agent = new Agent(GameManager.I.ActionSystem.CreateAIController(), pos + new Vector2(0.5f, 0.5f));
        agent.Init(member);
        agent.ShowUI();

        return agent;
    }
}