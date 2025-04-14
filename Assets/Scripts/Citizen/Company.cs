using System.Collections.Generic;

namespace Citizens
{
    public class Company
    {
        public string CompanyName { get; private set; }
        public Family Owner { get; private set; }
        public List<Property> Properties { get; } = new List<Property>();
        public Company(Family owner, string companyName = "Company")
        {
            Owner = owner;
            CompanyName = companyName + UnityEngine.Random.Range(0, 1000);
        }

        public void AddProperty(Property property)
        {
            Properties.Add(property);
        }
    }
}