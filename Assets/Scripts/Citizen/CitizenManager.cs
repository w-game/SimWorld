using System.Collections.Generic;
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
                randomPos + new Vector2(0.5f, 0.5f),
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

            return;

            List<IHouse> cityProperties = new List<IHouse>();
            List<Family> families = new List<Family>();

            foreach (var house in city.Houses)
            {
                if (house.HouseType == HouseType.House)
                {
                    house.TryGetFurnitures(out List<BedItem> beds);
                    if (beds.Count == 0) continue;
                    if (city.ChunkRand.Next(0, 100) < 20) continue;

                    var family = new Family();
                    family.AddHouse(house);

                    var familyTypes = _familyType.FindAll(_ => _.Length == beds.Count);
                    if (familyTypes.Count == 0) continue;
                    var selectedType = familyTypes[city.ChunkRand.Next(0, familyTypes.Count)];
                    var familyType = new List<string>(selectedType);

                    FamilyMember father, mother, grandfather, grandmother;
                    CreateParents(familyType, family, house, city, out father, out mother);
                    CreateGrandParents(familyType, family, house, city, father, mother, out grandfather, out grandmother);
                    CreateChildren(familyType, family, house, city, father, mother, grandfather, grandmother);

                    families.Add(family);
                }
                else if (house.HouseType == HouseType.Farm || house.HouseType == HouseType.Shop || house.HouseType == HouseType.Teahouse)
                {
                    cityProperties.Add(house);
                }
            }

            if (families.Count == 0) return;

            foreach (var property in cityProperties)
            {
                var family = families[city.ChunkRand.Next(0, families.Count)];
                AssignMembersJobs(family, property);
            }

            Families.Add(city, families);
        }

        private void AssignMembersJobs(Family family, IHouse house)
        {
            Property property = null;
            switch (house.HouseType)
            {
                case HouseType.Farm:
                    property = new FarmProperty(house, family);
                    break;
                case HouseType.Shop:
                    property = new ShopProperty(house, family);
                    break;
                case HouseType.Teahouse:
                    property = new TeahouseProperty(house, family);
                    break;
                default:
                    break;
            }

            var adults = family.Members.FindAll(_ => _.Age >= 18);
            if (adults.Count == 0)
            {
                return; // No adults to assign jobs
            }

            var ownerAmount = adults.Count > 1 ? Random.Range(1, adults.Count) : 1;

            if (ownerAmount == 1)
            {
                var randomAdult = adults[Random.Range(0, adults.Count)];
                var owner = new Owner(randomAdult);
                owner.AddProperty(property);
                randomAdult.SetJob(owner);
            }
            else
            {
                foreach (var adult in adults)
                {
                    var owner = new Owner(adult);
                    owner.AddProperty(property);
                    adult.SetJob(owner);
                }
            }

            house.SetOwner(family);
        }

        public FamilyMember CreatePlayer()
        {
            var player = new FamilyMember(true, 18);
            Family family = new Family();
            family.AddHouse(new House(new List<Vector2Int>(), HouseType.House));
            family.AddMember(player);
            return player;
        }

        internal List<Family> GetCitizens(City city)
        {
            return Families.TryGetValue(city, out var families) ? families : new List<Family>();
        }
    }
}