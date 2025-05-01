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

            var agent = GameItemManager.CreateGameItem<Agent>(
                null,
                randomPos + new Vector2(0.5f, 0.5f),
                GameItemType.Dynamic,
                GameManager.I.ActionSystem.CreateAIController(),
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

        public void GenerateNPCs(City city)
        {
            if (Families.ContainsKey(city))
            {
                return; // 如果城市已经有家庭，则不再生成
            }

            List<IHouse> cityProperties = new List<IHouse>();
            List<Family> families = new List<Family>();

            foreach (var house in city.Houses)
            {
                if (house.HouseType == HouseType.House)
                {
                    house.TryGetFurnitures(out List<BedItem> beds);
                    if (beds.Count == 0)
                    {
                        continue; // 如果没有床，则跳过
                    }
                    var family = new Family();
                    family.AddHouse(house);

                    var familyTypes = _familyType.FindAll(_ => _.Length == beds.Count);
                    if (familyTypes.Count == 0) continue;
                    var selectedType = familyTypes[city.ChunkRand.Next(0, familyTypes.Count - 1)];
                    var familyType = new List<string>(selectedType);

                    FamilyMember father = null;
                    FamilyMember mother = null;
                    
                    FamilyMember gradfather = null;
                    FamilyMember gradmother = null;

                    do
                    {
                        var adultItems = familyType.FindAll(_ => _ == "Adult");

                        if (adultItems.Count == 2)
                        {
                            father = CreateMember(family, house, true, city.ChunkRand.Next(20, 60));
                            mother = CreateMember(family, house, false, Mathf.Max(father.Age - city.ChunkRand.Next(0, 10), 20));
                            father.SetSpouse(mother);

                            familyType.Remove(adultItems[0]);
                            familyType.Remove(adultItems[1]);
                        }
                        else if (adultItems.Count == 1)
                        {
                            familyType.Remove(adultItems[0]);

                            mother = CreateMember(family, house, city.ChunkRand.Next(0, 100) > 50 ? true : false, city.ChunkRand.Next(20, 60));
                        }
                        else
                        {
                            var seniorItems = familyType.FindAll(_ => _ == "Senior");
                            if (seniorItems.Count == 2)
                            {
                                var baseAge = (father != null ? father.Age : mother.Age) + city.ChunkRand.Next(18, 35);
                                gradfather = CreateMember(family, house, true, baseAge + city.ChunkRand.Next(0, 10));
                                gradmother = CreateMember(family, house, false, baseAge);
                                gradfather.SetSpouse(gradmother);

                                gradfather.AddChild(father);
                                gradmother.AddChild(mother);

                                familyType.Remove(seniorItems[0]);
                                familyType.Remove(seniorItems[1]);
                            }
                            else if (seniorItems.Count == 1)
                            {
                                familyType.Remove(seniorItems[0]);
                                gradfather = CreateMember(family, house, true, city.ChunkRand.Next(60, 90));
                            }
                            else
                            {
                                var childItems = familyType.FindAll(_ => _ == "Child");
                                if (childItems.Count > 0)
                                {
                                    var maxAge = mother.Age - 18;
                                    foreach (var item in childItems)
                                    {
                                        var child = CreateMember(family, house, city.ChunkRand.Next(0, 100) > 50 ? true : false, city.ChunkRand.Next(0, maxAge));
                                        father?.AddChild(child);
                                        mother?.AddChild(child);

                                        gradfather?.AddGrandchild(child);
                                        gradmother?.AddGrandchild(child);

                                        familyType.Remove(item);
                                    }
                                }
                            }
                        }

                        city.ChangePopulation(1);
                    } while (familyType.Count > 0);

                    families.Add(family);
                }
                else if (house.HouseType == HouseType.Farm)
                {
                    cityProperties.Add(house);
                }
                else if (house.HouseType == HouseType.Shop)
                {
                    cityProperties.Add(house);
                } else if (house.HouseType == HouseType.Teahouse)
                {
                    cityProperties.Add(house);
                }
            }

            if (families.Count == 0)
            {
                return; // No families to assign jobs
            }

            foreach (var property in cityProperties)
            {
                var family = families[city.ChunkRand.Next(0, families.Count - 1)];
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
            family.AddMember(player);
            return player;
        }

        internal List<Family> GetCitizens(City city)
        {
            return Families.TryGetValue(city, out var families) ? families : new List<Family>();
        }
    }
}