using AI;
using Citizens;
using GameItem;

public class PutIntoBag : ActionBase
{
    public override float ProgressSpeed { get; protected set; } = 80f;
    public override int ProgressTimes { get; protected set; } = 1;

    private PropGameItem _gameItem;

    public override float CalculateUtility(AgentState state)
    {
        return 0f;
    }

    public PutIntoBag(PropGameItem gameItem)
    {
        _gameItem = gameItem;
        ActionName = "Put into Bag";
    }

    public override void OnRegister(AgentState state)
    {
        PrecedingActions.Add(new CheckMoveToTarget(_gameItem.transform.position));
    }

    protected override void DoExecute(AgentState state)
    {
        state.Agent.Bag.AddItem(_gameItem);
        MapManager.I.RemoveGameItem(_gameItem);
    }
}