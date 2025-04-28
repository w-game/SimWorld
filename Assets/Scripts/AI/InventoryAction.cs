using AI;
using Citizens;
using GameItem;

public class PutIntoBag : SingleActionBase
{
    private PropGameItem _gameItem;

    public override void OnGet(params object[] args)
    {
        _gameItem = args[0] as PropGameItem;
        ActionName = "Put into Bag";

        ActionSpeed = 10f;
    }

    public override void OnRegister(Agent agent)
    {
        PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _gameItem.Pos));
    }

    protected override void DoExecute(Agent agent)
    {
        agent.Bag.AddItem(_gameItem);
        _gameItem.Destroy();
    }
}