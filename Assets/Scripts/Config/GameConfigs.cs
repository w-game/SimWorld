using System;

[Serializable]
public class ConfigBase
{
    public string id;
    public string name;
    public string description;
    public string icon;
    public string prefab;
}


[Serializable]
public class BuildingConfig : ConfigBase
{
    public BuildingRequiredItem[] requiredItems;
}

[Serializable]
public class BuildingRequiredItem
{
    public int id;
    public int count;
}

[Serializable]
public class ConfigList<T>
{
    public T[] items;
}

[Serializable]
public class PropConfig : ConfigBase
{
    public int maxStackSize;

    public PropConfig(string id, string name, int maxStackSize)
    {
        this.id = id;
        this.name = name;
        this.maxStackSize = maxStackSize;
        this.icon = $"Icons/{id}";
        this.prefab = $"Prefabs/Props/{id}";
    }
}

[Serializable]
public class ResourceConfig : ConfigBase
{
    public string[] stages;
    public DropItem[] dropItems;
}

[Serializable]
public class DropItem
{
    public string id;
    public int count;
}

[Serializable]
public class GameItemToActions : ConfigBase
{
    public string[] actions;
}