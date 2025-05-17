using System;
using System.Collections.Generic;
using System.Linq;
using Citizens;
using Map;

public class PropertyManager : Singleton<PropertyManager>
{
    public List<Property> Properties { get; private set; } = new List<Property>();
    public Dictionary<Property, BusinessProperty> BusinessProperties { get; } = new Dictionary<Property, BusinessProperty>();
    public List<Property> PropertiesForRent { get; } = new List<Property>();
    public List<Property> PropertiesForSale { get; } = new List<Property>();
    public Dictionary<City, Dictionary<BusinessProperty, List<WorkType>>> PropertyRecruits { get; } = new Dictionary<City, Dictionary<BusinessProperty, List<WorkType>>>();

    public Property AddProperty(IHouse house, Family owner)
    {
        var property = new Property(house, owner);
        Properties.Add(property);
        return property;
    }

    public void RemoveProperty(Property property)
    {
        Properties.Remove(property);
    }

    internal void AddRecruit(BusinessProperty businessProperty, WorkType workType)
    {
        if (PropertyRecruits.TryGetValue(businessProperty.City, out var cityRecruits))
        {
            if (cityRecruits.TryGetValue(businessProperty, out var recruits))
            {
                recruits.Add(workType);
            }
            else
            {
                cityRecruits[businessProperty] = new List<WorkType> { workType };
            }
        }
        else
        {
            PropertyRecruits[businessProperty.City] = new Dictionary<BusinessProperty, List<WorkType>>
            {
                { businessProperty, new List<WorkType> { workType } }
            };
        }
    }

    public List<FarmProperty> GetFarmsForRent(Family owner)
    {
        return PropertiesForRent.Where(p => p.Owner == owner && p.House.HouseType == HouseType.Farm).Select(p => BusinessProperties[p] as FarmProperty).ToList();
    }

    public Work MatchRecruit(FamilyMember member, City city)
    {
        var allRecruitments = PropertyRecruits
            .Where(kvp => kvp.Key == city)
            .SelectMany(kvp => kvp.Value)
            .ToList();

        var randomProperty = allRecruitments
            .OrderBy(_ => Guid.NewGuid())
            .FirstOrDefault();

        Work work = null;
        WorkType randomRecruitment = default;
        if (!randomProperty.Equals(default(KeyValuePair<BusinessProperty, List<WorkType>>)))
        {
            randomRecruitment = randomProperty.Value
                .OrderBy(_ => Guid.NewGuid())
                .FirstOrDefault();
            switch (randomRecruitment)
            {
                case WorkType.FarmHelper:
                    work = new FarmHelper(member, randomProperty.Key);
                    break;
                case WorkType.Waiter:
                    work = new Waiter(member, randomProperty.Key);
                    break;
                case WorkType.Cooker:
                    work = new Cooker(member, randomProperty.Key);
                    break;
                case WorkType.Salesman:
                    work = new Salesman(member, randomProperty.Key);
                    break;
            }
        }

        if (work != null)
        {
            PropertyRecruits[city][randomProperty.Key].Remove(randomRecruitment);
            if (PropertyRecruits[city][randomProperty.Key].Count == 0)
            {
                PropertyRecruits[city].Remove(randomProperty.Key);
            }

            if (PropertyRecruits[city].Count == 0)
            {
                PropertyRecruits.Remove(city);
            }
        }

        return work;
    }
    
    public T GetOrCreateBusinessProperty<T>(Property property, City city) where T : BusinessProperty, new()
    {
        if (!BusinessProperties.TryGetValue(property, out var businessProperty))
        {
            businessProperty = new T();
            businessProperty.Init(property, city);
            BusinessProperties[property] = businessProperty;
        }

        return businessProperty as T;
    }
}