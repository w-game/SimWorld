using System;
using System.Collections.Generic;
using GameItem;
using Unity.VisualScripting;
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
        return AddItem(item.Config, item.Count);
    }

    public bool AddItem(PropConfig config, int quantity = 1)
    {
        var propItems = Items.FindAll(i => i.Config == config);
        if (propItems.Count > 0)
        {
            foreach (var propItem in propItems)
            {
                if (propItem.Quantity + quantity > config.maxStackSize)
                {
                    continue;
                }
                propItem.AddQuantity(quantity);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        if (Items.Count < MaxSize)
        {
            var newItem = new PropItem(config, quantity);
            Items.Add(newItem);
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    public void RemoveItem(PropConfig config, int quantity = 1)
    {
        var propItems = Items.FindAll(i => i.Config.id == config.id);
        foreach (var propItem in propItems)
        {
            if (propItem.Quantity > quantity)
            {
                propItem.AddQuantity(-quantity);
                OnInventoryChanged?.Invoke();
                return;
            }
            else
            {
                Items.Remove(propItem);
                OnInventoryChanged?.Invoke();
                return;
            }
        }
    }

    public void RemoveItem(PropItem item, int quantity = 1)
    {
        if (item.Quantity > quantity)
        {
            item.AddQuantity(-quantity);
            OnInventoryChanged?.Invoke();
            return;
        }
        else
        {
            Items.Remove(item);
            OnInventoryChanged?.Invoke();
            return;
        }
    }

    public List<PropItem> CheckItem(string id)
    {
        var items = Items.FindAll(i => i.Config.id == id);
        return items;
    }

    public int GetItem(PropConfig propConfig)
    {
        var propItems = Items.FindAll(i => i.Config.id == propConfig.id);

        int totalAmount = 0;
        foreach (var propItem in propItems)
        {
            totalAmount += propItem.Quantity;
        }
        return totalAmount;
    }
}