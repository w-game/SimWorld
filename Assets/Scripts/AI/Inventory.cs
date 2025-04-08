using System.Collections.Generic;

public class PropItem
{
    public PropConfig Config { get; private set; }
    public int Quantity { get; private set; }

    public PropItem(PropConfig config, int quantity = 1)
    {
        Config = config;
        Quantity = quantity;
    }
}

public class Inventory
{
    public List<PropItem> Items { get; private set; } = new List<PropItem>();
    public int MaxSize { get; private set; } = 20;

    public Inventory(int maxSize)
    {
        MaxSize = maxSize;
    }

    public bool AddItem(PropGameItem item)
    {
        if (Items.Count < MaxSize)
        {
            PropItem propItem = new PropItem(item.ConvtertConfig<PropConfig>(), item.Count);
            Items.Add(propItem);
            return true;
        }

        return false;
    }

    public void RemoveItem(PropItem item)
    {
        Items.Remove(item);
    }
}