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
    public Dictionary<BusinessProperty, List<WorkType>> PropertyRecruits { get; } = new Dictionary<BusinessProperty, List<WorkType>>();

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

    internal void AddRecruit(FarmProperty farmProperty, WorkType workType)
    {
        if (PropertyRecruits.TryGetValue(farmProperty, out var recruits))
        {
            recruits.Add(workType);
        }
        else
        {
            PropertyRecruits[farmProperty] = new List<WorkType> { workType };
        }
    }

    public List<FarmProperty> GetFarmsForRent(Family owner)
    {
        return PropertiesForRent.Where(p => p.Owner == owner && p.House.HouseType == HouseType.Farm).Select(p => BusinessProperties[p] as FarmProperty).ToList();
    }
}