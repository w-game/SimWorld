using System;
using System.Collections.Generic;
using Map;
using Newtonsoft.Json.Linq;
using UnityEngine;

[Serializable]
public class ConfigBase
{
    public string id;
    public string name;
    public string description;
    public string type;
    public string icon;
    public string prefab;
    public bool walkable = false;
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
    public int[] size;
    public BuildingRequiredItem[] requiredItems;
    public Dictionary<string, object> additionals;
}

[Serializable]
public class BuildingRequiredItem
{
    public int id;
    public int count;
}

[Serializable]
public class PropConfig : ConfigBase
{
    public int maxStackSize;
    public string category;
    public Dictionary<string, object> additionals;

    [NonSerialized]
    private CraftMaterialConfig[] _materials;
    public CraftMaterialConfig[] Materials
    {
        get
        {
            if (_materials != null) return _materials;

            if (additionals != null &&
                additionals.TryGetValue("materials", out var obj) &&
                obj is JArray jArr)                  // ConfigReader 默认把数组解析成 JArray
            {
                _materials = jArr.ToObject<CraftMaterialConfig[]>();
            }
            else
            {
                _materials = Array.Empty<CraftMaterialConfig>();
            }

            return _materials;
        }
    }

    private EffectConfig[] _effects;
    public EffectConfig[] Effects
    {
        get
        {
            if (_effects != null) return _effects;

            if (additionals != null &&
                additionals.TryGetValue("effects", out var obj) &&
                obj is JArray jArr)
            {
                _effects = jArr.ToObject<EffectConfig[]>();
            }
            else
            {
                _effects = Array.Empty<EffectConfig>();
            }

            return _effects;
        }
    }

    public int GetBasePrice()
    {
        if (additionals != null && additionals.ContainsKey("price"))
        {
            return int.Parse(additionals["price"].ToString());
        }
        return 0;
    }
}

[Serializable]
public class EffectConfig
{
    public string id;
    public string name;
    public float value;
}

[Serializable]
public class ResourceConfig : ConfigBase
{
    public string[] stages;
    public int matureDays;
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
    public int[] layout;
    public int width;
    public int height;
    public RoomFurniture[] furnitures;
}

[Serializable]
public class RoomFurniture
{
    public string id;
    public Vector2Int pos;
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

[Serializable]
public class BiomeConfig : ConfigBase
{
    public float scale;
    public float frequency;
    public LayerConfig[] layers;
}
[Serializable]
public class LayerConfig
{
    public float threshold;
    public string mode; // "cluster" or "scatter"
    public ItemWeight[] items;
}
[Serializable]
public class ItemWeight
{
    public string id;
    public float weight;
}