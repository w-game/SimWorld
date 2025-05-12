using System;
using System.Collections.Generic;
using System.Linq;
using GameItem;
using UI.Elements;
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

    public void AddQuantity(int delta)
    {
        Quantity = Math.Max(0, Quantity + delta);
    }
}

public class PropItem : PropItemBase
{
    public new PropConfig Config => base.Config as PropConfig;
    public PropType Type { get; private set; }
    public PropItem(ConfigBase config, int quantity = 1) : base(config, quantity)
    {
        var propConfig = config as PropConfig;
        Type = propConfig != null ? propConfig.type : PropType.None;
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

    /// <summary>Try stacking the given quantity into existing stacks and return the remaining amount.</summary>
    private int StackIntoExisting(PropConfig config, int quantity)
    {
        foreach (var propItem in Items)
        {
            if (propItem.Config != config || propItem.Quantity >= config.maxStackSize)
                continue;

            var space   = config.maxStackSize - propItem.Quantity;
            var delta   = Math.Min(space, quantity);
            propItem.AddQuantity(delta);
            OnInventoryChanged?.Invoke(propItem,  delta);
            quantity -= delta;

            if (quantity == 0)
                break;
        }
        return quantity;
    }

    /// <summary>Create a new stack and invoke change event.</summary>
    private void CreateNewStack(PropConfig config, int quantity)
    {
        var newItem = new PropItem(config, quantity);
        Items.Add(newItem);
        OnInventoryChanged?.Invoke(newItem, quantity);
    }

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
        // First try to merge into existing stacks.
        var remaining = StackIntoExisting(config, quantity);

        // If inventory is already full and nothing could be merged, fail fast.
        if (remaining > 0 && Items.Count >= MaxSize)
        {
            MessageBox.I.ShowMessage("Inventory is full.", "");
            return false;
        }

        // Create new stacks while we still have space and items left.
            while (remaining > 0 && Items.Count < MaxSize)
            {
                var q = Math.Min(remaining, config.maxStackSize);
                CreateNewStack(config, q);
                remaining -= q;
            }

        // Return true only if every item was added.
        return remaining == 0;
    }

    public bool RemoveItem(PropConfig config, int quantity = 1)
    {
        var targets = Items.Where(i => i.Config.id == config.id).ToList();
        foreach (var propItem in targets)
        {
            var delta = Math.Min(quantity, propItem.Quantity);
            propItem.AddQuantity(-delta);
            quantity -= delta;

            if (propItem.Quantity == 0)
                Items.Remove(propItem);

            OnInventoryChanged?.Invoke(propItem, -delta);

            if (quantity <= 0)
                return true;
        }
        return false;
    }

    public List<PropItem> CheckItem(string id)
    {
        var items = Items.FindAll(i => i.Config.id == id);
        return items;
    }

    public int CheckItemAmount(string id)
    {
        return Items.Where(i => i.Config.id == id).Sum(i => i.Quantity);
    }

    public int CheckItemAmount(PropType type)
    {
        return Items.Where(i => i.Type == type).Sum(i => i.Quantity);
    }

    public int GetItem(PropConfig propConfig) =>
        Items.Where(i => i.Config.id == propConfig.id).Sum(i => i.Quantity);

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

    public bool GetItemsByType(PropType type, out List<PropItem> items)
    {
        items = Items.Where(i => i.Type == type).ToList();
        return items.Count > 0;
    }
}