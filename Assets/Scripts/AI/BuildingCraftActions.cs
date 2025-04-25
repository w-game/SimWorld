using System;
using AI;
using Citizens;
using GameItem;
using UnityEngine;

public class CraftBuildingItemAction : ActionBase
{
    public override float ProgressSpeed { get; protected set; } = 1;
    public override int ProgressTimes { get; protected set; } = 1;

    private BlueprintItem _item;
    public CraftBuildingItemAction(BlueprintItem item)
    {
        ActionName = "Craft Building";
        _item = item;
    }

    public override void OnRegister(Agent agent)
    {
        foreach (var pos in _item.OccupiedPositions)
        {
            var items = GameManager.I.GameItemManager.GetItemsAtPos(new Vector3(pos.x, pos.y) + _item.Pos);
            foreach (var item in items)
            {
                if (item is PlantItem plantItem)
                {
                    var action = new RemovePlantAction(plantItem);
                    action.OnRegister(agent);
                    PrecedingActions.Add(action);
                }
            }
        }

        CheckMoveToArroundPos(agent, _item.Pos);
    }

    protected override void DoExecute(Agent agent)
    {
        var type = Type.GetType($"GameItem.{_item.Config.type}Item");
        GameItemManager.CreateGameItem<IGameItem>(type, _item.Config, _item.Pos, GameItemType.Static);
        GameItemManager.DestroyGameItem(_item);
    }
}

public class RemoveBuildingItemAction : ActionBase
{
    public override float ProgressSpeed { get; protected set; } = 1;
    public override int ProgressTimes { get; protected set; } = 1;

    private BuildingItem _item;
    public RemoveBuildingItemAction(BuildingItem item)
    {
        ActionName = "Remove Building";
        _item = item;
    }

    public override void OnRegister(Agent agent)
    {
        CheckMoveToArroundPos(agent, _item.Pos);
    }

    protected override void DoExecute(Agent agent)
    {
        _item.Destroy();
    }
}