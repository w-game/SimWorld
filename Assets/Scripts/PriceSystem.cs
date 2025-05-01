using System.Collections.Generic;
using Map;
using UnityEngine;

public class CityPrice
{
    public Dictionary<string, int> CurrentInventory { get; } = new Dictionary<string, int>();
    public Dictionary<string, int> PriceDict { get; } = new Dictionary<string, int>();
}

public class PriceSystem
{
    public int cityPopulation = 1000;

    public float priceUpdateInterval = 24 * 60 * 60; // 7 days

    private float _priceUpdateTimer;

    private Dictionary<City, CityPrice> _cityPrices = new Dictionary<City, CityPrice>();


    public PriceSystem()
    {
        UpdatePrices();
    }

    public void Update()
    {
        _priceUpdateTimer += GameTime.DeltaTime;
        if (_priceUpdateTimer >= priceUpdateInterval)
        {
            UpdatePrices();
            _priceUpdateTimer = 0f;
        }
    }

    public CityPrice AddCity(City city)
    {
        if (!_cityPrices.ContainsKey(city))
        {
            _cityPrices[city] = new CityPrice();
            city.OnPopulationChanged += (newPop) =>
            {
                cityPopulation = newPop;
                UpdatePrices();
            };
        }
        return _cityPrices[city];
    }

    public void SetCurrentInventory(City city, string itemId, int qty)
    {
        _cityPrices[city].CurrentInventory[itemId] = qty;
    }

    private void UpdatePrices()
    {
        var configs = ConfigReader.GetAllConfigs<PropConfig>();

        foreach (var city in _cityPrices.Keys)
        {
            var cityPrice = _cityPrices[city];
            foreach (var cfg in configs)
            {
                var basePrice = (int)cfg.GetBasePrice();
                int curQty = cityPrice.CurrentInventory.ContainsKey(cfg.id) ? cityPrice.CurrentInventory[cfg.id] : 0;
                float factor = (float)cityPopulation / Mathf.Max(curQty, 1);
                cityPrice.PriceDict[cfg.id] = (int)(basePrice * factor);
            }
        }
    }

    public int GetPrice(City city, string itemId)
    {
        if (!_cityPrices.ContainsKey(city))
        {
            return ConfigReader.GetConfig<PropConfig>(itemId).GetBasePrice();
        }
        if (_cityPrices[city].PriceDict.TryGetValue(itemId, out int price))
        {
            return price;
        }
        Debug.LogWarning($"PriceSystem: No price found for ItemId '{itemId}'");
        return 0;
    }
}