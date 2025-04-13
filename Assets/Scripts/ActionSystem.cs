
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
        public event Action<List<ActionBase>, Vector3> OnMouseClick;

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

                if (items.Count > 0)
                {
                    Log.LogInfo("ActionSystem", "Click on item");
                    var actions = ItemsToActions(items);
                    // TODO: 叠加手上道具行为

                    if (actions.Count > 0)
                    {
                        OnMouseClick?.Invoke(actions, Input.mousePosition);
                        Debug.Log("Mouse Clicked");
                    }
                }
                else
                {
                    Log.LogInfo("ActionSystem", "Click on map");
                    var blockType = MapManager.I.CheckBlockType(mousePos);
                    var buildingType = MapManager.I.CheckBuildingType(mousePos);

                    var actions = BuildingToActions(mousePos, buildingType);
                    // TODO: 叠加手上道具行为

                    switch (blockType)
                    {
                        case BlockType.Plain:
                            actions.Add(new CheckMoveToTarget(mousePos));
                            actions.Add(new HoeAction(mousePos));
                            actions.Add(new StartBuildingCraftAction());

                            OnMouseClick?.Invoke(actions, Input.mousePosition);
                            break;
                        case BlockType.Ocean:
                            break;
                    }

                }
            }
        }

        private List<ActionBase> ItemsToActions(List<IGameItem> items)
        {
            List<ActionBase> actions = new List<ActionBase>();
            foreach (var item in items)
            {
                switch (item)
                {
                    case PropGameItem propGameItem:
                        if (propGameItem is FoodItem foodItem)
                        {
                            actions.Add(new EatAction(foodItem, GameManager.I.CurrentAgent.State.Hunger));
                        }

                        actions.Add(new PutIntoBag(propGameItem));
                        actions.Add(new CheckMoveToTarget(propGameItem.Pos));
                        break;
                    case PlantItem plantItem:
                        if (plantItem is TreeItem treeItem)
                        {
                        }
                        else
                        {
                            actions.Add(new CheckMoveToTarget(plantItem.Pos));
                        }
                        actions.Add(new RemovePlantAction(plantItem));
                        break;
                    default:
                        break;
                }

                var config = GameManager.I.ConfigReader.GetConfig<GameItemToActions>(item.Config.id);
                if (config == null)
                {
                    continue;
                }
                foreach (var action in config.actions)
                {
                    var actionType = Type.GetType($"AI.{action}Action");
                    if (actionType != null)
                    {
                        var actionInstance = Activator.CreateInstance(actionType, new object[] { item }) as ActionBase;
                        if (actionInstance != null)
                        {
                            actions.Add(actionInstance);
                        }
                    }
                }
            }

            return actions;
        }

        private List<ActionBase> BuildingToActions(Vector3 pos, BuildingType buildingType)
        {
            List<ActionBase> actions = new List<ActionBase>();
            switch (buildingType)
            {
                case BuildingType.Wall:
                    actions.Add(new CheckMoveToTarget(Vector3.zero));
                    break;
                case BuildingType.Farm:
                    actions.Add(new PlantAction(pos, "PROP_SEED_"));
                    break;
                case BuildingType.None:
                    break;
            }

            return actions;
        }

        internal void RegisterAction(ActionBase action)
        {
            GameManager.I.CurrentAgent.Brain.RegisterAction(action, true);
        }
    }
}