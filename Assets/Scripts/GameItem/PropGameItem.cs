using GameItem;

public class PropGameItem : GameItemBase
{
    public int Count { get; private set; } = 1;

    public void Init(PropConfig config, int count)
    {
        base.Init(config);
        Count = count;
    }
}