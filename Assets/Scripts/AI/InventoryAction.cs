using AI;
using Citizens;
using GameItem;

public class PutIntoBag : SingleActionBase
{
    private PropGameItem _gameItem;

    public PutIntoBag(PropGameItem gameItem)
    {
        _gameItem = gameItem;
        ActionName = "Put into Bag";
    }

    public override void OnRegister(Agent agent)
    {
        PrecedingActions.Add(new CheckMoveToTarget(agent, _gameItem.Pos));
    }

    protected override void DoExecute(Agent agent)
    {
        agent.Bag.AddItem(_gameItem);
        _gameItem.Destroy();
    }
}