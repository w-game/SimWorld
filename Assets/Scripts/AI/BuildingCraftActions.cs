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

    public override float CalculateUtility(AgentState state)
    {
        return 0;
    }

    public override void OnRegister(AgentState state)
    {
        
    }

    protected override void DoExecute(AgentState state)
    {
        PopBuildingCraft.StartCraft();
    }
}