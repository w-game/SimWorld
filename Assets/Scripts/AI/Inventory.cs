using System.Collections.Generic;

public class PropItem
{
    public PropConfig Config { get; private set; }
    public int Quantity { get; private set; }

    public PropItem(PropConfig config)
    {
        Config = config;
        Quantity = 1; // Default quantity is 1 when created
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

    public bool AddItem(PropItem item)
    {
        if (Items.Count < MaxSize)
        {
            Items.Add(item);
            return true;
        }

        return false;
    }

    public void RemoveItem(PropItem item)
    {
        Items.Remove(item);
    }
}