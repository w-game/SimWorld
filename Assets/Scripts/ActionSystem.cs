
using System;
using System.Collections.Generic;
using GameItem;
using Map;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AI
{
    public interface IActionDetector
    {
        List<IAction> DetectActions();
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
                    actions.AddRange(item.ClickItemActions());
                    if (!item.Walkable)
                    {
                        isWalkable = false;
                    }
                }

                var blockType = MapManager.I.CheckBlockType(mousePos);
                actions.AddRange(BlockTypeToActions(mousePos, blockType));

                if (isWalkable && blockType != BlockType.Ocean)
                {
                    actions.Add(new CheckMoveToTarget(mousePos));
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
                    // actions.Add(new HoeAction());
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