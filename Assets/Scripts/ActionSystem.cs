using System;
using System.Collections.Generic;
using Map;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AI
{
    public interface IActionPool
    {
        void OnGet(params object[] args);
        void OnRelease();
    }

    public static class ActionPool
    {
        // Pool storage per action type
        private static readonly Dictionary<Type, Stack<IActionPool>> _pools = new Dictionary<Type, Stack<IActionPool>>();

        public static T Get<T>(params object[] args) where T : class, IActionPool
        {
            var type = typeof(T);
            var action = Get(type, args);
            return action as T;
        }

        public static IAction Get(Type type, params object[] args)
        {
            IActionPool action;
            if (_pools.TryGetValue(type, out var stack) && stack.Count > 0)
            {
                action = stack.Pop();
            }
            else
            {
                action = Activator.CreateInstance(type) as IActionPool;
            }
            action.OnGet(args);
            return action as IAction;
        }

        public static void Release(IActionPool action)
        {
            if (action == null) return;
            action.OnRelease(); // default resets state
            var type = action.GetType();
            if (!_pools.TryGetValue(type, out var stack))
            {
                stack = new Stack<IActionPool>();
                _pools[type] = stack;
            }
            stack.Push(action);
        }
    }
    public class ActionSystem
    {
        public event Action<Dictionary<string, List<IAction>>, Vector3> OnMouseClick;

        internal void Init()
        {

        }

        private static readonly Vector3 SIGN_OFFSET = new Vector3(0.5f, 0.5f, 0);

        /// <summary>Moves the selection sign to the centre of the given cell.</summary>
        private static void UpdateSelectSign(Vector2Int cellPos)
        {
            GameManager.I.selectSign.transform.position =
                new Vector3(cellPos.x, cellPos.y, 0) + SIGN_OFFSET;
        }

        /// <summary>Builds the click‐action map for the given world position.</summary>
        private Dictionary<string, List<IAction>> BuildActions(Vector3 worldPos, Vector2Int cellPos)
        {
            var actions = new Dictionary<string, List<IAction>>();

            // 1) Dynamic (moving) item takes priority.
            var dynamicItem = GameManager.I.GameItemManager.CheckDynamicItems(worldPos);
            if (dynamicItem != null && dynamicItem != GameManager.I.CurrentAgent)
            {
                var human = new List<IAction>();
                human.AddRange(dynamicItem.ActionsOnClick(GameManager.I.CurrentAgent));
                actions["Human"] = human;
            }

            // 2) Static items at this location.
            bool isWalkable = true;
            foreach (var item in GameManager.I.GameItemManager.GetItemsAtPos(worldPos))
            {
                actions[item.ConfigBase.name] = item.ActionsOnClick(GameManager.I.CurrentAgent);
                if (!item.Walkable) isWalkable = false;
            }

            // 3) Terrain / ground actions.
            var blockType = MapManager.I.CheckBlockType(worldPos);
            if (isWalkable && blockType != BlockType.Ocean)
            {
                if (!actions.ContainsKey("Ground"))
                    actions["Ground"] = new List<IAction>();
                actions["Ground"].Add(
                    ActionPool.Get<CheckMoveToTarget>(GameManager.I.CurrentAgent, worldPos));
            }

            var extraGround = BlockTypeToActions(worldPos, blockType);
            if (extraGround.Count > 0)
            {
                if (!actions.ContainsKey("Ground"))
                    actions["Ground"] = new List<IAction>();
                actions["Ground"].AddRange(extraGround);
            }

            return actions;
        }

        internal void Update()
        {
            // Ignore input while crafting or when the pointer is over UI.
            if (BuildingManager.I.CraftMode || EventSystem.current.IsPointerOverGameObject())
                return;

            var mouseWorldPos = UIManager.I.MousePosToWorldPos();
            var cellPos       = MapManager.I.WorldPosToCellPos(mouseWorldPos);

            UpdateSelectSign(cellPos);

            if (!Input.GetMouseButtonDown(1)) return;

            var actions = BuildActions(mouseWorldPos, cellPos);
            if (actions.Count == 0) return;

            GameManager.I.selectSign.SetActive(true);
            OnMouseClick?.Invoke(actions, Input.mousePosition);
        }

        private List<IAction> BlockTypeToActions(Vector3 pos, BlockType blockType)
        {
            List<IAction> actions = new List<IAction>();
            switch (blockType)
            {
                case BlockType.Plain:
                    if (GameManager.I.GameItemManager.GetItemsAtPos(pos).Count == 0)
                    {
                        IHouse house = GameManager.I.CurrentAgent.Citizen.Family.GetHouse(HouseType.Farm);
                        var cellPos = MapManager.I.WorldPosToCellPos(pos);
                        actions.Add(ActionPool.Get<HoeAction>(new Vector3(cellPos.x, cellPos.y, 0), house));
                    }
                    break;
                case BlockType.Road:
                case BlockType.Forest:
                case BlockType.Mountain:
                case BlockType.Desert:
                    break;
                case BlockType.Ocean:
                    break;
            }

            return actions;
        }

        internal void RegisterAction(IAction action)
        {
            GameManager.I.CurrentAgent.Brain.RegisterAction(action, true);
        }
    }
}