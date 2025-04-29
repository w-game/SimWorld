
using System;
using System.Collections.Generic;
using GameItem;
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
            return action as T;
        }

        public static void Release(IActionPool action)
        {
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
        public event Action<List<IAction>, Vector3> OnMouseClick;

        internal void Init()
        {

        }

        public AIController CreateAIController()
        {
            return new AIController();
        }

        internal void Update()
        {
            if (BuildingManager.I.CraftMode)
                return;

            var mousePos = UIManager.I.MousePosToWorldPos();
            var cellPos = MapManager.I.WorldPosToCellPos(mousePos);
            GameManager.I.selectSign.transform.position = new Vector3(cellPos.x, cellPos.y, 0) + new Vector3(0.5f, 0.5f, 0);

            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                Log.LogInfo("ActionSystem", "MousePos: " + cellPos);
                GameManager.I.selectSign.SetActive(true);

                var items = GameManager.I.GameItemManager.GetItemsAtPos(mousePos);

                var actions = new List<IAction>();
                var isWalkable = true;
                foreach (var item in items)
                {
                    actions.AddRange(item.ActionsOnClick(GameManager.I.CurrentAgent));
                    if (!item.Walkable)
                    {
                        isWalkable = false;
                    }
                }

                var blockType = MapManager.I.CheckBlockType(mousePos);
                actions.AddRange(BlockTypeToActions(mousePos, blockType));

                if (isWalkable && blockType != BlockType.Ocean)
                {
                    actions.Add(ActionPool.Get<CheckMoveToTarget>(GameManager.I.CurrentAgent, mousePos));
                }

                OnMouseClick?.Invoke(actions, Input.mousePosition);
            }
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