using System;
using System.Collections.Generic;
using System.Linq;
using GameItem;
using UnityEngine.Events;

public class PropItemBase
{
    public ConfigBase Config { get; private set; }
    public int Quantity { get; private set; }

    public PropItemBase(ConfigBase config, int quantity = 1)
    {
        Config = config;
        Quantity = quantity;
    }

    public void AddQuantity(int quantity)
    {
        Quantity += quantity;
    }
}

public class PropItem : PropItemBase
{
    public new PropConfig Config => base.Config as PropConfig;
    public PropType Type { get; private set; }
    public PropItem(ConfigBase config, int quantity = 1) : base(config, quantity)
    {
        Type = (PropType)Enum.Parse(typeof(PropType), config.type);
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
    Fruit,
    Crop,
    Tool,
    ProcessedCrop,
    Weapon,
    Ore,
    Ingredient
}

public class Inventory
{
    public List<PropItem> Items { get; private set; } = new List<PropItem>();
    public int MaxSize { get; private set; } = 20;

    public event UnityAction<PropItem, int> OnInventoryChanged;

    public Inventory(int maxSize)
    {
        MaxSize = maxSize;
    }

    public bool AddItem(PropItem item)
    {
        return AddItem(item.Config, item.Quantity);
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
                OnInventoryChanged?.Invoke(propItem, quantity);
                return true;
            }
        }

        if (Items.Count < MaxSize)
        {
            if (quantity > config.maxStackSize)
            {
                var totalQuantity = quantity;
                var itemCount = totalQuantity / config.maxStackSize;
                if (totalQuantity % config.maxStackSize > 0)
                {
                    itemCount++;
                }
                for (int i = 0; i < itemCount; i++)
                {
                    var q = totalQuantity > config.maxStackSize ? config.maxStackSize : totalQuantity;
                    totalQuantity -= q;
                    var newItem = new PropItem(config, q);
                    Items.Add(newItem);
                    OnInventoryChanged?.Invoke(newItem, q);
                }
            }
            else
            {
                var newItem = new PropItem(config, quantity);
                Items.Add(newItem);
                OnInventoryChanged?.Invoke(newItem, quantity);
            }
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
                OnInventoryChanged?.Invoke(propItem, -quantity);
                return;
            }
            else
            {
                Items.Remove(propItem);
                OnInventoryChanged?.Invoke(propItem, -propItem.Quantity);
                return;
            }
        }
    }

    public void RemoveItem(PropItem item, int quantity = 1)
    {
        if (item.Quantity > quantity)
        {
            item.AddQuantity(-quantity);
            OnInventoryChanged?.Invoke(item, -quantity);
            return;
        }
        else
        {
            Items.Remove(item);
            OnInventoryChanged?.Invoke(item, -item.Quantity);
            return;
        }
    }

    public List<PropItem> CheckItem(string id)
    {
        var items = Items.FindAll(i => i.Config.id == id);
        return items;
    }

    public int CheckItemAmount(string id)
    {
        var items = Items.FindAll(i => i.Config.id == id);
        return items.Sum(i => i.Quantity);
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
        var propItems = Items.FindAll(i => i.Config.additionals != null && i.Config.additionals.ContainsKey(type));
        if (propItems.Count == 0)
        {
            return null;
        }

        float maxValue = float.MinValue;
        PropItem item = null;
        foreach (var propItem in propItems)
        {
            foreach (var addition in propItem.Config.additionals)
            {
                if (addition.Key == type)
                {
                    if ((float)addition.Value > maxValue)
                    {
                        maxValue = (float)addition.Value;
                        item = propItem;
                    }
                }
            }
        }
        return item;
    }
}