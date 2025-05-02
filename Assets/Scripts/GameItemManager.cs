using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI;
using Citizens;
using GameItem;
using UnityEngine;

public class GameItemManager
{
    private static Dictionary<Vector2Int, List<IGameItem>> _staticGameItems = new Dictionary<Vector2Int, List<IGameItem>>();
    private static List<IGameItem> _dynamicGameItems = new List<IGameItem>();

    private static readonly float HideUISqrDistance = 64f * 64f;
    private static readonly float DestroySqrDistance = 64f * 64f;

    public static event Func<Vector3, Vector2Int> ItemPosToMapPosConverter;

    public ObjectPool ItemUIPool { get; private set; }

    private static List<(List<IGameItem>, List<IGameItem>)> _gameItems = new List<(List<IGameItem>, List<IGameItem>)>();
    private const int itemPerBatch = 30;

    public GameItemManager(Func<Vector3, Vector2Int> func)
    {
        ItemPosToMapPosConverter = func;
        ItemUIPool = new ObjectPool(128);

        for (int i = 0; i < 10; i++)
        {
            _gameItems.Add((new List<IGameItem>(), new List<IGameItem>()));
            GameManager.I.StartCoroutine(GameItemUpdateCoroutine(_gameItems[i]));
        }
    }

    public static T CreateGameItem<T>(ConfigBase config, Vector3 pos, GameItemType itemType, params object[] otherObjs) where T : class, IGameItem
    {
        return CreateGameItem<T>(typeof(T), config, pos, itemType, otherObjs);
    }

    public static T CreateGameItem<T>(Type type, ConfigBase config, Vector3 pos, GameItemType itemType, params object[] otherObjs) where T : class, IGameItem
    {
        object[] ctorArgs = new object[2 + otherObjs.Length];
        ctorArgs[0] = config;
        ctorArgs[1] = ItemPosToMapPosConverter.Invoke(pos).ToVector3();
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

        var batch = _gameItems.FirstOrDefault(x => x.Item1.Count < itemPerBatch);
        if (batch.Item1 == null || batch.Item2 == null)
        {
            batch = (new List<IGameItem>(), new List<IGameItem>());
            _gameItems.Add(batch);
            GameManager.I.StartCoroutine(GameItemUpdateCoroutine(batch));
        }

        batch.Item2.Add(item);
        item.ShowUI();

        return item;
    }

    public static bool DestroyGameItem(IGameItem gameItem)
    {
        switch (gameItem.ItemType)
        {
            case GameItemType.Static:
                UnregisterStaticGameItem(gameItem);
                break;
            case GameItemType.Dynamic:
                UnregisterDynamicGameItem(gameItem);
                break;
        }

        gameItem.Destroy();
        gameItem.Active = false;
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
                    _staticGameItems.Remove(pos);
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
            return new List<IGameItem>(_staticGameItems[mapPos]);
        }
        return new List<IGameItem>();
    }

    public T TryGetItemAtPos<T>(Vector3 pos) where T : class, IGameItem
    {
        var mapPos = ItemPosToMapPosConverter.Invoke(pos);
        if (_staticGameItems.ContainsKey(mapPos))
        {
            return _staticGameItems[mapPos].FirstOrDefault(x => x is T) as T;
        }
        return null;
    }

    private static IEnumerator GameItemUpdateCoroutine((List<IGameItem>, List<IGameItem>) value)
    {
        List<IGameItem> batch = value.Item1;
        List<IGameItem> addBatch = value.Item2;
        List<IGameItem> itemToKill = new List<IGameItem>();
        while (true)
        {
            foreach (var gameItem in batch)
            {
                if (gameItem.Active)
                {
                    gameItem.DoUpdate();
                }
                else
                {
                    itemToKill.Add(gameItem);
                }
            }

            foreach (var item in itemToKill)
            {
                batch.Remove(item);
            }
            itemToKill.Clear();

            foreach (var gameItem in addBatch)
            {
                if (gameItem.Active)
                {
                    batch.Add(gameItem);
                }
            }
            addBatch.Clear();
            yield return null;
        }
    }

    internal Agent CreateNPC(Vector2Int pos, FamilyMember member)
    {
        var agent = CreateGameItem<Agent>(
            null,
            pos + new Vector2(0.5f, 0.5f),
            GameItemType.Dynamic,
            new AIController(),
            member
        );
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

    internal IGameItem CheckDynamicItems(Vector3 pos)
    {
        var cellPos = ItemPosToMapPosConverter.Invoke(pos);
        float dis = float.MaxValue;
        IGameItem closestItem = null;
        foreach (var item in _dynamicGameItems)
        {
            if (item.Active)
            {
                var sqrDistance = (item.Pos - pos).sqrMagnitude;
                if (sqrDistance < dis)
                {
                    dis = sqrDistance;
                    closestItem = item;
                }
            }
        }

        if (closestItem != null)
        {
            var itemCellPos = ItemPosToMapPosConverter.Invoke(closestItem.Pos);
            if ((cellPos - itemCellPos).sqrMagnitude < 2)
            {
                return closestItem;
            }
        }
        return null;
    }
}