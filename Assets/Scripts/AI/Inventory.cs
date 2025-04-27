using System;
using System.Collections.Generic;
using System.Linq;
using GameItem;
using UnityEngine;
using UnityEngine.Events;

public class PropItemBase
{
    public ConfigBase Config { get; private set; }
    public int Quantity { get; private set; }
    public PropType Type { get; private set; }

    public PropItemBase(ConfigBase config, int quantity = 1)
    {
        Config = config;
        Quantity = quantity;
        Type = Enum.Parse<PropType>(config.type);
    }

    public void AddQuantity(int quantity)
    {
        Quantity += quantity;
    }
}

public class PropItem : PropItemBase
{
    public new PropConfig Config => base.Config as PropConfig;
    public PropItem(ConfigBase config, int quantity = 1) : base(config, quantity)
    {
    }
}

public class CraftPropItem : PropItemBase
{
    public new CraftConfig Config => base.Config as CraftConfig;
    public CraftPropItem(ConfigBase config, int quantity = 1) : base(config, quantity)
    {
    }
}

public enum PropType
{
    None,
    Food,
    Seed,
    Material,
    Tool,
    Equipment,
    Weapon,
    Crop
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

    public bool AddItem(PropItem item)
    {
        return AddItem(item.Config as PropConfig, item.Quantity);
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

    internal PropItem GetItemHasEffect(string type)
    {
        var propItems = Items.FindAll(i => i.Config.effects != null && i.Config.effects.Any(e => e.type == type));
        if (propItems.Count == 0)
        {
            return null;
        }

        float maxValue = float.MinValue;
        PropItem item = null;
        foreach (var propItem in propItems)
        {
            foreach (var effect in propItem.Config.effects)
            {
                if (effect.type == type)
                {
                    if (effect.value > maxValue)
                    {
                        maxValue = effect.value;
                        item = propItem;
                    }
                }
            }
        }
        return item;
    }
}