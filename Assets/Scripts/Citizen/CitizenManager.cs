using System.Collections.Generic;
using System.Linq;
using AI;
using GameItem;
using Map;
using UnityEngine;

namespace Citizens
{
    public class CitizenManager
    {
        public Dictionary<City, List<Family>> Families { get; } = new Dictionary<City, List<Family>>();

        private List<string[]> _familyType = new List<string[]>()
        {
            new string[] { "Adult" },
            new string[] { "Adult", "Adult" },
            new string[] { "Adult", "Adult", "Child" },
            new string[] { "Adult", "Adult", "Child", "Child" },
            new string[] { "Adult", "Adult", "Child", "Child", "Child" },
            new string[] { "Adult", "Child" },
            new string[] { "Adult", "Child", "Child" },
            new string[] { "Senior", "Adult", "Adult" },
            new string[] { "Senior", "Adult", "Adult", "Child" },
            new string[] { "Senior", "Adult", "Adult", "Child", "Child" },
            new string[] { "Senior", "Adult", "Adult", "Child", "Child", "Child" },
            new string[] { "Senior", "Senior", "Adult", "Adult" },
            new string[] { "Senior", "Senior", "Adult", "Adult", "Child" },
            new string[] { "Senior", "Senior", "Adult", "Adult", "Child", "Child" },
            new string[] { "Senior", "Senior", "Adult", "Adult", "Child", "Child", "Child" }
        };

        private FamilyMember CreateMember(Family family, IHouse house, bool sex, int age)
        {
            var member = new FamilyMember(sex, age);
            family.AddMember(member);

            var randomPos = new Vector2(Random.Range(house.MinPos.x + 1, house.MinPos.x + house.Size.x - 2), Random.Range(house.MinPos.y + 1, house.MinPos.y + house.Size.y - 2));

            var agent = GameItemManager.CreateGameItem<AgentNPC>(
                null,
                randomPos,
                GameItemType.Dynamic,
                new AIController(),
                member
            );

            agent.RegisterSchedule(
                new Schedule(
                    20 * 60 * 60,
                    8 * 60 * 60,
                    new List<int>() { 1, 2, 3, 4, 5, 6, 7 },
                    ActionPool.Get<SleepAction>(agent.State.Sleep),
                    member), "Sleep");

            return member;
        }

        private void CreateParents(List<string> familyType, Family family, IHouse house, City city, out FamilyMember father, out FamilyMember mother)
        {
            father = null;
            mother = null;
            var adults = familyType.FindAll(_ => _ == "Adult");

            if (adults.Count >= 2)
            {
                father = CreateMember(family, house, true, city.ChunkRand.Next(20, 60));
                mother = CreateMember(family, house, false, Mathf.Max(father.Age - city.ChunkRand.Next(0, 10), 20));
                father.SetSpouse(mother);
                familyType.Remove("Adult");
                familyType.Remove("Adult");
            }
            else if (adults.Count == 1)
            {
                mother = CreateMember(family, house, city.ChunkRand.Next(0, 100) > 50, city.ChunkRand.Next(20, 60));
                familyType.Remove("Adult");
            }
        }

        private void CreateGrandParents(List<string> familyType, Family family, IHouse house, City city, FamilyMember father, FamilyMember mother, out FamilyMember grandfather, out FamilyMember grandmother)
        {
            grandfather = null;
            grandmother = null;
            var seniors = familyType.FindAll(_ => _ == "Senior");

            if (seniors.Count >= 2)
            {
                var baseAge = Mathf.Max(father?.Age ?? mother?.Age ?? 40, 40) + city.ChunkRand.Next(18, 35);
                grandfather = CreateMember(family, house, true, baseAge + city.ChunkRand.Next(0, 10));
                grandmother = CreateMember(family, house, false, baseAge);
                grandfather.SetSpouse(grandmother);
                if (father != null) grandfather.AddChild(father);
                if (mother != null) grandmother.AddChild(mother);
                familyType.Remove("Senior");
                familyType.Remove("Senior");
            }
            else if (seniors.Count == 1)
            {
                grandfather = CreateMember(family, house, true, city.ChunkRand.Next(60, 90));
                familyType.Remove("Senior");
            }
        }

        private void CreateChildren(List<string> familyType, Family family, IHouse house, City city, FamilyMember father, FamilyMember mother, FamilyMember grandfather, FamilyMember grandmother)
        {
            var children = familyType.FindAll(_ => _ == "Child");
            int maxAge = Mathf.Max((mother?.Age ?? 40) - 18, 1);

            foreach (var _ in children)
            {
                var child = CreateMember(family, house, city.ChunkRand.Next(0, 100) > 50, city.ChunkRand.Next(0, maxAge));
                father?.AddChild(child);
                mother?.AddChild(child);
                grandfather?.AddGrandchild(child);
                grandmother?.AddGrandchild(child);
                familyType.Remove("Child");
            }
        }

        public void GenerateNPCs(City city)
        {
            if (Families.ContainsKey(city)) return;

            List<Family> families = new List<Family>();

            var houses = city.Houses.Where(h => h.HouseType == HouseType.House);
            var propertyHouses = city.Houses.Where(h => h.HouseType != HouseType.House);

            var remainingHouses = houses.ToList();

            foreach (var house in houses)
            {
                var prob = city.ChunkRand.Next(0, 100);
                if (prob < 10) continue;
                remainingHouses.Remove(house);
                house.TryGetFurnitures(out List<BedItem> beds);
                if (beds.Count == 0) continue;
                if (city.ChunkRand.Next(0, 100) < 20) continue;

                var family = new Family();
                var home = PropertyManager.I.AddProperty(house, family);
                family.Properties.Add(home);

                var familyTypes = _familyType.FindAll(_ => _.Length == beds.Count);
                if (familyTypes.Count == 0) continue;
                var selectedType = familyTypes[city.ChunkRand.Next(0, familyTypes.Count)];
                var familyType = new List<string>(selectedType);

                FamilyMember father, mother, grandfather, grandmother;
                CreateParents(familyType, family, house, city, out father, out mother);
                CreateGrandParents(familyType, family, house, city, father, mother, out grandfather, out grandmother);
                CreateChildren(familyType, family, house, city, father, mother, grandfather, grandmother);

                family.Members.ForEach(m => m.SetHome(home));

                var headProb = city.ChunkRand.Next(0, 100);
                family.SetHead(headProb < 40 ? father ?? mother ?? grandfather ?? grandmother : grandfather ?? father ?? mother ?? grandmother);
                families.Add(family);
                city.ChangePopulation(family.Members.Count);
            }

            foreach (var propertyHouse in propertyHouses)
            {
                var family = families[city.ChunkRand.Next(0, families.Count)];
                var property = InitProperty(family, propertyHouse);
            }

            InitWork(families, city);

            Families.Add(city, families);
        }

        private Property InitProperty(Family family, IHouse house)
        {
            var adults = family.Members.Where(m => m.Age >= 18).ToList();
            if (adults.Count == 0) return null;

            var property = PropertyManager.I.AddProperty(house, family);
            family.Properties.Add(property);
            return property;
        }

        private void InitWork(List<Family> families, City city)
        {
            var familiesHaveProperty = families.Where(f => f.Properties.Count(p => p.House.HouseType != HouseType.House) > 0).ToList();

            foreach (var family in familiesHaveProperty)
            {
                var properties = family.Properties.Where(p => p.House.HouseType != HouseType.House && p.House.HouseType != HouseType.Farm).ToList();
                var farms = family.Properties.Where(p => p.House.HouseType == HouseType.Farm).ToList();

                if (properties.Count > 0)
                {
                    var adults = family.Members.Where(m => m.Age >= 18).ToList();
                    foreach (var property in properties)
                    {
                        if (adults.Count == 0) continue;
                        var adult = adults[Random.Range(0, adults.Count)];
                        adults.Remove(adult);

                        BusinessProperty businessProperty = null;
                        switch (property.House.HouseType)
                        {
                            case HouseType.Restaurant:
                                businessProperty = new RestaurantProperty(property, city);
                                break;
                            case HouseType.Farm:
                                businessProperty = new FarmProperty(property, city);
                                break;
                            case HouseType.Teahouse:
                                businessProperty = new TeahouseProperty(property, city);
                                break;
                            case HouseType.Shop:
                                businessProperty = new ShopProperty(property, city);
                                break;
                        }

                        var ceo = new CEO(adult, businessProperty);
                        adult.SetWork(ceo);
                    }

                    var prob = Random.Range(0, 100);
                    if (prob < 50)
                    {
                        foreach (var farm in farms)
                        {
                            PropertyManager.I.PropertiesForRent.Add(farm);
                        }
                    }
                    else
                    {
                        foreach (var farm in farms)
                        {
                            var farmProperty = new FarmProperty(farm, city);
                            farmProperty.AddRecruitCount(WorkType.FarmHelper);
                            PropertyManager.I.AddRecruit(farmProperty, WorkType.FarmHelper);
                        }
                    }
                }
                else
                {
                    var totalArea = farms.Sum(f => f.House.Size.x * f.House.Size.y);

                    if (totalArea < 100)
                    {
                        var adults = family.Members.Where(m => m.Age >= 18).ToList();
                        foreach (var adult in adults)
                        {
                            var work = new Farmer(adult, farms.Select(f => new FarmProperty(f, city)).ToList());
                            adult.SetWork(work);
                        }
                    }
                    else
                    {
                        var prob = Random.Range(0, 100);
                        if (prob < 50)
                        {
                            foreach (var farm in farms)
                            {
                                PropertyManager.I.PropertiesForRent.Add(farm);
                            }
                        }
                        else
                        {
                            foreach (var farm in farms)
                            {
                                var farmProperty = new FarmProperty(farm, city);
                                farmProperty.AddRecruitCount(WorkType.FarmHelper);
                                PropertyManager.I.AddRecruit(farmProperty, WorkType.FarmHelper);
                            }
                        }
                    }
                }
            }

            var familiesNoProperty = families.Where(f => f.Properties.All(p => p.House.HouseType == HouseType.House) && f.Properties.Count == 1).ToList();
            foreach (var family in familiesNoProperty)
            {
                var adults = family.Members.Where(m => m.Age >= 18).ToList();
                if (adults.Count == 0) continue;
                var adult = adults[Random.Range(0, adults.Count)];
                adults.Remove(adult);
                PropertyManager.I.MatchRecruit(adult, city);
            }
        }

        public FamilyMember CreatePlayer()
        {
            var player = new FamilyMember(true, 18);
            Family family = new Family();
            family.AddMember(player);
            return player;
        }

        internal List<Family> GetCitizens(City city)
        {
            return Families.TryGetValue(city, out var families) ? families : new List<Family>();
        }
    }
}