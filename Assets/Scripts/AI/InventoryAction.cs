using AI;
using Citizens;

public class PutIntoBag : ActionBase
{
    public override float ProgressSpeed { get; protected set; } = 80f;
    public override int ProgressTimes { get; protected set; } = 1;

    private PropGameItem _gameItem;

    public PutIntoBag(PropGameItem gameItem)
    {
        _gameItem = gameItem;
        ActionName = "Put into Bag";
    }

    public override void OnRegister(Agent agent)
    {
        PrecedingActions.Add(new CheckMoveToTarget(_gameItem.Pos));
    }

    protected override void DoExecute(Agent agent)
    {
        agent.Bag.AddItem(_gameItem);
        _gameItem.Destroy();
    }
}