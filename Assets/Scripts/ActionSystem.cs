
using System;
using System.Collections.Generic;
using AI;
using GameItem;
using Map;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActionSystem
{
    public event Action<List<ActionBase>, Vector3> OnMouseClick;

    internal void Init()
    {

    }

    internal void Update()
    {
        if (BuildingManager.I.CraftMode)
            return;
            
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            var mousePos = UIManager.I.MousePosToWorldPos();

            Log.LogInfo("ActionSystem", "MousePos: " + MapManager.I.WorldPosToCellPos(mousePos));
            var items = MapManager.I.GetItemsAtPos(mousePos);

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

    private List<ActionBase> ItemsToActions(List<MonoGameItem> items)
    {
        List<ActionBase> actions = new List<ActionBase>();
        foreach (var item in items)
        {
            actions.AddRange(GameItemActions.GetActionByItem(item));
        }
        
        return actions;
    }

    private List<ActionBase> BuildingToActions(Vector3 pos, BuildingType buildingType)
    {
        List<ActionBase> actions = new List<ActionBase>();
        switch (buildingType)
        {
            case BuildingType.House:
                actions.Add(new CheckMoveToTarget(Vector3.zero));
                break;
            case BuildingType.Farm:
                actions.Add(new PlantAction(pos, 0));
                break;
            case BuildingType.None:
                break;
        }

        return actions;
    }

    internal void RegisterAction(ActionBase action)
    {
        GameManager.I.CurrentAgent.RegisterAction(action);
    }
}