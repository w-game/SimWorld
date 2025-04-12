using Citizens;
using GameItem;
using UnityEngine;

public class PropGameItem : GameItemBase
{
    public int Count { get; private set; } = 1;

    public Family Owner { get; private set; }

    public PropGameItem(ConfigBase config, int count, Vector3 pos = default) : base(config, pos)
    {
        Count = count;
    }
}