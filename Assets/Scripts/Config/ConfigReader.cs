using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic JSON‑based configuration loader.
/// Add a new *Config* type + resource file and call <see cref="LoadConfigs"/> –
/// no extra code changes are necessary.
/// </summary>
public class ConfigReader
{
    /// <remarks>
    /// Key = Config type (e.g. typeof(BuildingConfig))
    /// Value = all loaded configs of that type
    /// </remarks>
    private readonly Dictionary<Type, List<ConfigBase>> _configs = new();

    /// <summary>Expose read‑only view if external systems need it.</summary>
    public IReadOnlyDictionary<Type, List<ConfigBase>> Configs => _configs;

    /// <summary>
    /// Mapping between a config <see cref="Type"/> and its Resources path.
    /// Add new entries here when introducing a new config file.
    /// </summary>
    private static readonly Dictionary<Type, string> TypeToPath = new()
    {
        { typeof(BuildingConfig)   , "Configs/BuildingConfig"    },
        { typeof(PropConfig)       , "Configs/PropConfig"        },
        { typeof(ResourceConfig)   , "Configs/ResourceConfig"    },
        { typeof(GameItemToActions), "Configs/GameItemToActions" },
        { typeof(RoomConfig)       , "Configs/RoomConfig"        },
        { typeof(JobConfig)        , "Configs/JobConfig"         }
    };

    /// <summary>Load/refresh every registered config file.</summary>
    internal void LoadConfigs()
    {
        _configs.Clear();

        foreach (var (type, path) in TypeToPath)
        {
            // Use reflection to invoke the generic method below with the correct T.
            GetType()
                .GetMethod(nameof(LoadConfig), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(type)
                .Invoke(this, new object[] { path });
        }
    }

    /// <summary>
    /// Load one concrete config list and cache it under its <typeparamref name="T"/> key.
    /// </summary>
    private void LoadConfig<T>(string path) where T : ConfigBase
    {
        var textAsset = Resources.Load<TextAsset>(path);
        if (textAsset == null)
            throw new Exception($"Config file not found: {path}");

        var listWrapper = JsonUtility.FromJson<ConfigList<T>>(textAsset.text);
        if (listWrapper == null || listWrapper.items == null)
            throw new Exception($"Failed to parse config list in: {path}");

        _configs[typeof(T)] = new List<ConfigBase>(listWrapper.items);
    }

    /// <summary>Return a single config by id.</summary>
    public T GetConfig<T>(string id) where T : ConfigBase
    {
        if (!_configs.TryGetValue(typeof(T), out var list))
            throw new Exception($"{typeof(T).Name} not loaded. Did you call LoadConfigs()?");

        var cfg = list.Find(c => c.id == id) as T;
        if (cfg == null)
            throw new Exception($"{typeof(T).Name} with id '{id}' not found.");

        return cfg;
    }

    /// <summary>Return all configs of a given type.</summary>
    public List<T> GetAllConfigs<T>() where T : ConfigBase
    {
        if (!_configs.TryGetValue(typeof(T), out var list))
            throw new Exception($"{typeof(T).Name} not loaded. Did you call LoadConfigs()?");

        // Safe because we only store instances of T under this key.
        return list.ConvertAll(c => (T)c);
    }

    #region Helper wrapper used by JsonUtility
    [Serializable]
    private class ConfigList<T> where T : ConfigBase
    {
        public List<T> items = new();
    }
    #endregion
}