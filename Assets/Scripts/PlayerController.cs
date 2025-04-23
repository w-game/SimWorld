using Citizens;
using GameItem;

public class PlayerController : GameItemUI
{
    private Agent _agent;

    public override void Init(GameItemBase gameItem)
    {
        _agent = gameItem as Agent;
    }
}