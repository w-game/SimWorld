using System.Collections.Generic;
using Map;
using UnityEngine;

namespace Citizens
{
    public class CitizenManager
    {
        public Dictionary<City, List<Family>> Families { get; } = new Dictionary<City, List<Family>>();
        public Dictionary<City, Dictionary<Family, Company>> Companies { get; } = new Dictionary<City, Dictionary<Family, Company>>();

        private FamilyMember CreateMember(Family family, House house, bool sex, int age)
        {
            var member = new FamilyMember(sex, age);
            var agent = new Agent(GameManager.I.ActionSystem.CreateAIController(), house.RandomPos + new Vector2(0.5f, 0.5f));
            agent.Init(member);
            family.AddMember(member);

            return member;
        }

        public void GenerateNPCs(City city)
        {
            if (Families.ContainsKey(city))
            {
                return; // 如果城市已经有家庭，则不再生成
            }

            List<House> cityProperties = new List<House>();
            List<Family> families = new List<Family>();

            foreach (var house in city.Houses)
            {
                if (house.HouseType == HouseType.House)
                {
                    Family family = new Family();
                    var familyType = city.ChunkRand.Next(0, 100);
                    family.AddHouse(house);

                    if (familyType < 15)
                    {
                        // Single adult living alone.
                        int age = city.ChunkRand.Next(18, 60);
                        CreateMember(family, house, true, age);
                    }
                    else if (familyType < 30)
                    {
                        // Single parent family with 1-3 children.
                        int parentAge = city.ChunkRand.Next(25, 50);
                        var adult = CreateMember(family, house, true, parentAge);
                        int childrenCount = city.ChunkRand.Next(1, 4); // 1 to 3 children
                        for (int i = 0; i < childrenCount; i++)
                        {
                            int childAge = city.ChunkRand.Next(0, 18);
                            var child = CreateMember(family, house, false, childAge);
                            adult.AddChild(child);
                        }
                    }
                    else if (familyType < 55)
                    {
                        // Nuclear family: two adults and 1-3 children.
                        int adultAge = city.ChunkRand.Next(25, 50);
                        var father = CreateMember(family, house, true, adultAge);
                        var mother = CreateMember(family, house, false, adultAge - city.ChunkRand.Next(0, 5));
                        father.SetSpouse(mother);
                        int childrenCount = city.ChunkRand.Next(1, 4); // 1 to 3 children
                        for (int i = 0; i < childrenCount; i++)
                        {
                            int childAge = city.ChunkRand.Next(0, 18);
                            var child = CreateMember(family, house, false, childAge);
                            father.AddChild(child);
                            mother.AddChild(child);
                        }
                    }
                    else if (familyType < 80)
                    {
                        // Extended family: couple, children, and a senior member.
                        int adultAge = city.ChunkRand.Next(25, 50);
                        var father = CreateMember(family, house, true, adultAge);
                        var mother = CreateMember(family, house, false, adultAge - city.ChunkRand.Next(0, 5));
                        father.SetSpouse(mother);

                        int seniorAge = city.ChunkRand.Next(60, 90);
                        var senior = CreateMember(family, house, city.ChunkRand.Next(0, 100) < 50, seniorAge);
                        senior.AddChild(father);
                        senior.AddChild(mother);

                        int childrenCount = city.ChunkRand.Next(2, 5); // 2 to 4 children
                        for (int i = 0; i < childrenCount; i++)
                        {
                            int childAge = city.ChunkRand.Next(0, 18);
                            var child = CreateMember(family, house, false, childAge);
                            father.AddChild(child);
                            mother.AddChild(child);
                            senior.AddGrandchild(child);
                        }
                    }
                    else
                    {
                        // Diverse household: a mix of adults and children (e.g., roommates or mixed-age group).
                        int numMembers = city.ChunkRand.Next(2, 6); // Between 2 and 5 members
                        for (int i = 0; i < numMembers; i++)
                        {
                            bool isAdult = city.ChunkRand.Next(0, 100) < 70;
                            int age = isAdult ? city.ChunkRand.Next(18, 60) : city.ChunkRand.Next(0, 18);
                            CreateMember(family, house, city.ChunkRand.Next(0, 100) < 50, age);
                        }
                    }

                    families.Add(family);
                }
                else if (house.HouseType == HouseType.Farm)
                {
                    cityProperties.Add(house);
                }
                else if (house.HouseType == HouseType.Shop)
                {
                    cityProperties.Add(house);
                }
            }


            Companies.Add(city, new Dictionary<Family, Company>());
            if (families.Count == 0)
            {
                return; // No families to assign jobs
            }

            foreach (var property in cityProperties)
            {
                var family = families[city.ChunkRand.Next(0, families.Count - 1)];
                AssignMembersJobs(Companies[city], family, property);
            }

            foreach (var family in families)
            {
                if (!Companies.ContainsKey(city))
                {
                    AssignMembersJobs(city, family);
                }
            }

            Families.Add(city, families);
        }

        private void AssignMembersJobs(Dictionary<Family, Company> companies, Family family, House house)
        {
            var property = new Property(house, family);
            if (!companies.TryGetValue(family, out var company))
            {
                company = new Company(family);
                company.AddProperty(property);
                companies.Add(family, company);
            }

            company.AddProperty(property);
        }

        private void AssignMembersJobs(City city, Family family)
        {

        }

        public FamilyMember CreatePlayer()
        {
            var player = new FamilyMember(true, 18);
            return player;
        }

        internal List<Family> GetCitizens(City city)
        {
            return Families.TryGetValue(city, out var families) ? families : new List<Family>();
        }
    }
}