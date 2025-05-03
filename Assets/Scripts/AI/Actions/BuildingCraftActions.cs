using System;
using AI;
using Citizens;
using GameItem;
using UnityEngine;

public class CraftBuildingItemAction : SingleActionBase
{
    private BlueprintItem _item;

    public override void OnGet(params object[] args)
    {
        _item = args[0] as BlueprintItem;
        ActionName = "Craft Building";

        ActionSpeed = 1f;
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
                    AddPrecedingAction<RemovePlantAction>(agent, plantItem);
                }
            }
        }

        CheckMoveToArroundPos(agent, _item.Pos, () => { Target = _item.Pos; });
    }

    protected override void DoExecute(Agent agent)
    {
        var type = Type.GetType($"GameItem.{_item.Config.type}Item");
        var item = GameItemManager.CreateGameItem<IGameItem>(type, _item.Config, _item.Pos, GameItemType.Static);
        item.Owner = agent.Owner;
        GameItemManager.DestroyGameItem(_item);
    }
}

public class RemoveBuildingItemAction : SingleActionBase
{
    private BuildingItem _item;

    public override void OnGet(params object[] args)
    {
        _item = args[0] as BuildingItem;
        ActionName = "Remove Building";

        ActionSpeed = 1f;
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