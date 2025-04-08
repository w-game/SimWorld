using GameItem;

public class TreeItem : PlantItem<ResourceConfig>
{
    public override string ItemName => "TreeItem";

    public override void Init(ResourceConfig config)
    {
        base.Init(config);
    }
}