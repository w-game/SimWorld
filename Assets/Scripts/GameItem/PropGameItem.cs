using GameItem;

public class PropGameItem : GameItemBase<PropConfig>
{
    public override string ItemName => "道具";
    public int Count { get; private set; } = 1;

    public void Init(PropConfig config, int count)
    {
        base.Init(config);
        Count = count;
    }
}