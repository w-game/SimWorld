using System;
using System.Collections.Generic;
using GameItem;
using UnityEngine.Events;

public class PropItem
{
    public PropConfig Config { get; private set; }
    public int Quantity { get; private set; }

    public PropItem(PropConfig config, int quantity = 1)
    {
        Config = config;
        Quantity = quantity;
    }

    public void AddQuantity(int quantity)
    {
        Quantity += quantity;
    }
}

public class Inventory
{
    public List<PropItem> Items { get; private set; } = new List<PropItem>();
    public int MaxSize { get; private set; } = 20;

    public event UnityAction OnInventoryChanged;

    public Inventory(int maxSize)
    {
        MaxSize = maxSize;
    }

    public bool AddItem(PropGameItem item)
    {
        var propItems = Items.FindAll(i => i.Config == item.Config);
        if (propItems.Count > 0)
        {
            foreach (var propItem in propItems)
            {
                if (propItem.Quantity + item.Count > item.Config.maxStackSize)
                {
                    continue;
                }
                propItem.AddQuantity(item.Count);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        if (Items.Count < MaxSize)
        {
            var newItem = new PropItem(item.Config, item.Count);
            Items.Add(newItem);
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    public void RemoveItem(PropItem item)
    {
        Items.Remove(item);
    }

    internal List<PropItem> CheckItem(string id)
    {
        var items = Items.FindAll(i => i.Config.id == id);
        return items;
    }
}