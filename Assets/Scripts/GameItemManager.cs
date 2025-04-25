using System;
using System.Collections.Generic;
using System.Linq;
using Citizens;
using GameItem;
using UnityEngine;

public class GameItemManager
{
    private static Dictionary<Vector2Int, List<IGameItem>> _staticGameItems = new Dictionary<Vector2Int, List<IGameItem>>();
    private static List<IGameItem> _dynamicGameItems = new List<IGameItem>();

    public static event Func<Vector3, Vector2Int> ItemPosToMapPosConverter;

    public ObjectPool<GameItemUI> ItemUIPool { get; private set; }

    public GameItemManager(Func<Vector3, Vector2Int> func)
    {
        ItemPosToMapPosConverter = func;
        ItemUIPool = new ObjectPool<GameItemUI>(128);
    }

    public static T CreateGameItem<T>(ConfigBase config, Vector3 pos, GameItemType itemType, params object[] otherObjs) where T : class, IGameItem
    {
        return CreateGameItem<T>(typeof(T), config, pos, itemType, otherObjs);
    }

    public static T CreateGameItem<T>(Type type, ConfigBase config, Vector3 pos, GameItemType itemType, params object[] otherObjs) where T : class, IGameItem
    {
        // Build a single argument list: (config, pos, ...otherObjs)
        object[] ctorArgs = new object[2 + otherObjs.Length];
        ctorArgs[0] = config;
        ctorArgs[1] = pos;
        Array.Copy(otherObjs, 0, ctorArgs, 2, otherObjs.Length);

        var item = Activator.CreateInstance(type, ctorArgs) as T;
        item.CalcSize();
        item.ItemType = itemType;
        switch (itemType)
        {
            case GameItemType.Static:
                RegisterStaticGameItem(item);
                break;
            case GameItemType.Dynamic:
                RegisterDynamicGameItem(item);
                break;
        }
        return item;
    }

    public static bool DestroyGameItem(IGameItem gameItem)
    {
        gameItem.Destroy();
        switch (gameItem.ItemType)
        {
            case GameItemType.Static:
                UnregisterStaticGameItem(gameItem);
                break;
            case GameItemType.Dynamic:
                UnregisterDynamicGameItem(gameItem);
                break;
        }
        return true;
    }

    private static void RegisterStaticGameItem(IGameItem gameItem)
    {
        foreach (var mapPos in gameItem.OccupiedPositions)
        {
            var pos = mapPos + ItemPosToMapPosConverter.Invoke(gameItem.Pos);
            
            if (!_staticGameItems.ContainsKey(pos))
                _staticGameItems[pos] = new List<IGameItem>();

            _staticGameItems[pos].Add(gameItem);
        }
    }

    private static void RegisterDynamicGameItem(IGameItem gameItem)
    {
        _dynamicGameItems.Add(gameItem);
    }

    private static void UnregisterStaticGameItem(IGameItem gameItem)
    {
        foreach (var mapPos in gameItem.OccupiedPositions)
        {
            var pos = mapPos + ItemPosToMapPosConverter.Invoke(gameItem.Pos);
            if (_staticGameItems.ContainsKey(pos))
            {
                _staticGameItems[pos].Remove(gameItem);
                if (_staticGameItems[pos].Count == 0)
                {
                    _staticGameItems.Remove(pos);
                }
            }
        }
    }

    private static void UnregisterDynamicGameItem(IGameItem gameItem)
    {
        _dynamicGameItems.Remove(gameItem);
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
        var uniqueStaticItems = new HashSet<IGameItem>(_staticGameItems.Values.SelectMany(x => x));
        foreach (var gameItem in uniqueStaticItems)
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
        var agent = CreateGameItem<Agent>(
            null,
            pos + new Vector2(0.5f, 0.5f),
            GameItemType.Dynamic,
            GameManager.I.ActionSystem.CreateAIController()
        );
        agent.Init(member);
        agent.ShowUI();

        return agent;
    }

    public void SwitchType(IGameItem sign, GameItemType type)
    {
        sign.ItemType = type;
        switch (type)
        {
            
            case GameItemType.Static:
                UnregisterDynamicGameItem(sign);
                RegisterStaticGameItem(sign);
                break;
            case GameItemType.Dynamic:
                UnregisterStaticGameItem(sign);
                RegisterDynamicGameItem(sign);
                break;
        }
    }
}