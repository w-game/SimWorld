using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Citizens;
using DG.Tweening;
using GameItem;
using UnityEngine;
using UnityEngine.Events;

public class GameItemManager
{
    private static Dictionary<Vector2Int, List<IGameItem>> _staticGameItems = new Dictionary<Vector2Int, List<IGameItem>>();
    private static List<IGameItem> _dynamicGameItems = new List<IGameItem>();

    private static HashSet<IGameItem> _uniqueStaticItems = new HashSet<IGameItem>();
    private static HashSet<IGameItem> _uniqueDynamicItems = new HashSet<IGameItem>();
    private static List<IGameItem> _itemsToDestroyStatic = new List<IGameItem>();
    private static List<IGameItem> _itemsToDestroyDynamic = new List<IGameItem>();
    private static readonly float HideUISqrDistance = 64f * 64f;
    private static readonly float DestroySqrDistance = 64f * 64f;

    public static event Func<Vector3, Vector2Int> ItemPosToMapPosConverter;

    public ObjectPool ItemUIPool { get; private set; }

    public GameItemManager(Func<Vector3, Vector2Int> func)
    {
        ItemPosToMapPosConverter = func;
        ItemUIPool = new ObjectPool(128);

        GameManager.I.StartCoroutine(LoopUpdateStatic());
        GameManager.I.StartCoroutine(LoopUpdateDynamic());
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

        return item;
    }

    public static bool DestroyGameItem(IGameItem gameItem)
    {
        switch (gameItem.ItemType)
        {
            case GameItemType.Static:
                _itemsToDestroyStatic.Add(gameItem);
                break;
            case GameItemType.Dynamic:
                _itemsToDestroyDynamic.Add(gameItem);
                break;
        }
        return true;
    }

    public static void DestroyGameItem(IGameItem gameItem, GameItemType type)
    {
        switch (type)
        {
            case GameItemType.Static:
                UnregisterStaticGameItem(gameItem);
                gameItem.Destroy();
                break;
            case GameItemType.Dynamic:
                UnregisterDynamicGameItem(gameItem);
                gameItem.Destroy();
                break;
        }
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
            return _staticGameItems[mapPos];
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

    private void UpdateUniqueStaticItems()
    {
        _uniqueStaticItems.Clear();
        foreach (var list in _staticGameItems.Values)
        {
            foreach (var item in list)
            {
                _uniqueStaticItems.Add(item);
            }
        }
    }

    public void UpdateUniqueDynamicItems()
    {
        _uniqueDynamicItems.Clear();
        foreach (var item in _dynamicGameItems)
        {
            _uniqueDynamicItems.Add(item);
        }
    }

    private IEnumerator LoopUpdateStatic(int maxCount = 1000)
    {
        UpdateUniqueStaticItems();
        int count = maxCount;
        while (true)
        {
            maxCount = count;
            int totalCount = _uniqueStaticItems.Count();

            if (totalCount < maxCount)
                maxCount = totalCount;

            if (maxCount == 0)
                yield return null;

            _itemsToDestroyStatic.Clear();
            var agentPos = GameManager.I.CurrentAgent.Pos;

            int itemCount = 0;
            foreach (var gameItem in _uniqueStaticItems)
            {
                gameItem.DoUpdate();

                float sqrDis = (gameItem.Pos - agentPos).sqrMagnitude;
                if (sqrDis > DestroySqrDistance)
                {
                    _itemsToDestroyStatic.Add(gameItem);
                    continue;
                }

                if (sqrDis > HideUISqrDistance)
                {
                    gameItem.HideUI();
                }
                else if (gameItem.UI == null)
                {
                    gameItem.ShowUI();
                }

                itemCount++;
                if (itemCount >= maxCount) // 每一帧只处理100个物品
                {
                    yield return null;
                    itemCount = 0;
                }
            }

            foreach (var item in _itemsToDestroyStatic)
            {
                DestroyGameItem(item, GameItemType.Static);
            }

            UpdateUniqueStaticItems();
            yield return null;
        }
    }

    private IEnumerator LoopUpdateDynamic(int maxCount = 1000)
    {
        UpdateUniqueDynamicItems();
        int count = maxCount;
        while (true)
        {
            maxCount = count;
            int totalCount = _uniqueDynamicItems.Count();

            if (totalCount < maxCount)
                maxCount = totalCount;

            if (maxCount == 0)
                yield return null;

            _itemsToDestroyDynamic.Clear();
            var agentPos = GameManager.I.CurrentAgent.Pos;

            int itemCount = 0;
            foreach (var gameItem in _uniqueDynamicItems)
            {
                gameItem.DoUpdate();

                float sqrDis = (gameItem.Pos - agentPos).sqrMagnitude;
                if (sqrDis > DestroySqrDistance)
                {
                    _itemsToDestroyDynamic.Add(gameItem);
                    continue;
                }

                if (sqrDis > HideUISqrDistance)
                {
                    gameItem.HideUI();
                }
                else if (gameItem.UI == null)
                {
                    gameItem.ShowUI();
                }

                itemCount++;
                if (itemCount >= maxCount) // 每一帧只处理 maxCount 个物品
                {
                    yield return null;
                    itemCount = 0;
                }
            }

            foreach (var item in _itemsToDestroyDynamic)
            {
                DestroyGameItem(item, GameItemType.Dynamic);
            }

            UpdateUniqueDynamicItems();
            yield return null;
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