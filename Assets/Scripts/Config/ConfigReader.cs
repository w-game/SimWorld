using System;
using System.Collections.Generic;
using UnityEngine;

public class ConfigReader
{
    public Dictionary<string, List<ConfigBase>> Configs { get; private set; }

    internal void LoadConfigs()
    {
        Configs = new Dictionary<string, List<ConfigBase>>();
        LoadConfig<BuildingConfig>("Configs/BuildingConfig");
        LoadConfig<PropConfig>("Configs/PropConfig");
        LoadConfig<ResourceConfig>("Configs/ResourceConfig");
        LoadConfig<GameItemToActions>("Configs/GameItemToActions");
        LoadConfig<RoomConfig>("Configs/RoomConfig");
    }

    private void LoadConfig<T>(string path) where T : ConfigBase
    {
        var configListText = Resources.Load<TextAsset>(path);
        if (configListText == null)
        {
            throw new Exception($"Config file not found: {path}");
        }
        var json = configListText.text;
        var configList = JsonUtility.FromJson<ConfigList<T>>(json);
        
        Configs.Add(path, new List<ConfigBase>());
        foreach (var config in configList.items)
        {
            Configs[path].Add(config);
        }
    }

    public T GetConfig<T>(string id) where T : ConfigBase
    {
        switch (typeof(T).Name)
        {
            case nameof(BuildingConfig):
                Configs.TryGetValue("Configs/BuildingConfig", out var buildingConfigs);
                if (buildingConfigs == null)
                {
                    throw new Exception($"BuildingConfig not found");
                }
                return buildingConfigs.Find(config => config.id == id) as T;
            case nameof(PropConfig):
                Configs.TryGetValue("Configs/PropConfig", out var propConfigs);
                if (propConfigs == null)
                {
                    throw new Exception($"PropConfig not found");
                }
                return propConfigs.Find(config => config.id == id) as T;
            case nameof(ResourceConfig):
                Configs.TryGetValue("Configs/ResourceConfig", out var resourceConfigs);
                if (resourceConfigs == null)
                {
                    throw new Exception($"ResourceConfig not found");
                }
                return resourceConfigs.Find(config => config.id == id) as T;
            case nameof(GameItemToActions):
                Configs.TryGetValue("Configs/GameItemToActions", out var gameItemToActions);
                if (gameItemToActions == null)
                {
                    throw new Exception($"GameItemToActions not found");
                }
                return gameItemToActions.Find(config => config.id == id) as T;
            case nameof(RoomConfig):
                Configs.TryGetValue("Configs/RoomConfig", out var roomConfigs);
                if (roomConfigs == null)
                {
                    throw new Exception($"RoomConfig not found");
                }
                return roomConfigs.Find(config => config.id == id) as T;
            default:
                throw new Exception($"Unsupported config type: {typeof(T).Name}");
        }
    }
    
    public List<T> GetAllConfigs<T>() where T : ConfigBase
    {
        switch (typeof(T).Name)
        {
            case nameof(BuildingConfig):
                Configs.TryGetValue("Configs/BuildingConfig", out var buildingConfigs);
                if (buildingConfigs == null)
                {
                    throw new Exception($"BuildingConfig not found");
                }
                return buildingConfigs.ConvertAll(config => (T)config);
            case nameof(PropConfig):
                Configs.TryGetValue("Configs/PropConfig", out var propConfigs);
                if (propConfigs == null)
                {
                    throw new Exception($"PropConfig not found");
                }
                return propConfigs.ConvertAll(config => (T)config);
            case nameof(ResourceConfig):
                Configs.TryGetValue("Configs/ResourceConfig", out var resourceConfigs);
                if (resourceConfigs == null)
                {
                    throw new Exception($"ResourceConfig not found");
                }
                return resourceConfigs.ConvertAll(config => (T)config);
            case nameof(GameItemToActions):
                Configs.TryGetValue("Configs/GameItemToActions", out var gameItemToActions);
                if (gameItemToActions == null)
                {
                    throw new Exception($"GameItemToActions not found");
                }
                return gameItemToActions.ConvertAll(config => (T)config);
            case nameof(RoomConfig):
                Configs.TryGetValue("Configs/RoomConfig", out var roomConfigs);
                if (roomConfigs == null)
                {
                    throw new Exception($"RoomConfig not found");
                }
                return roomConfigs.ConvertAll(config => (T)config);
            default:
                throw new Exception($"Unsupported config type: {typeof(T).Name}");
        }
    }
}