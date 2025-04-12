using AI;
using Citizens;

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