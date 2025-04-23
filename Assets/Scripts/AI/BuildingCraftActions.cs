using AI;
using Citizens;
using GameItem;
using UnityEngine;

public class StartBuildingCraftAction : ActionBase
{
    public override float ProgressSpeed { get; protected set; } = 0;
    public override int ProgressTimes { get; protected set; } = -1;

    public StartBuildingCraftAction()
    {
        ActionName = "Building Craft";
    }

    public override void OnRegister(Agent agent)
    {

    }

    protected override void DoExecute(Agent agent)
    {
        PopBuildingCraft.StartCraft();
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
        CheckMoveToArroundPos(agent, _item);
    }

    protected override void DoExecute(Agent agent)
    {
        _item.Destroy();
    }
}