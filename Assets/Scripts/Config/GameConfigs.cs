using System;
using Map;

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
public class PosConfig
{
    public int x;
    public int y;
}

[Serializable]
public class BuildingConfig : ConfigBase
{
    public string type;
    public int[] size;    
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

[Serializable]
public class RoomConfig : ConfigBase
{
    public string type;
    public int[] layout;
    public int width;
    public int height;
    public RoomFurniture[] furnitures;
}

[Serializable]
public class RoomFurniture
{
    public string id;
    public int[] pos;
}

[Serializable]
public class JobConfig : ConfigBase
{

}

[Serializable]
public class CraftConfig : ConfigBase
{
    public CraftMaterialConfig[] materials;
}

[Serializable]
public class CraftMaterialConfig
{
    public string id;
    public int amount;
}