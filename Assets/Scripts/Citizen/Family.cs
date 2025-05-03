using System;
using System.Collections.Generic;
using AI;
using GameItem;
using Map;
using UnityEngine;

namespace Citizens
{
    public class Family
    {
        public List<IHouse> Houses { get; } = new List<IHouse>(); // 房屋

        public List<FamilyMember> Members { get; } = new List<FamilyMember>();
        public List<IAction> Actions { get; } = new List<IAction>(); // 行为

        public void AddMember(FamilyMember member)
        {
            Members.Add(member);
            member.Family = this;
        }

        public void RemoveMember(FamilyMember member)
        {
            Members.Remove(member);
        }

        internal void AddHouse(IHouse house)
        {
            Houses.Add(house);
            house.SetOwner(this);
        }

        internal IHouse GetHouse(HouseType houseType)
        {
            var house = Houses.Find(h => h.HouseType == houseType);

            if (house != null)
            {
                return house;
            }
            else
            {
                house = new House(new List<Vector2Int>(), houseType);
                house.SetOwner(this);
                Houses.Add(house);
                return house;
            }
        }
    }

    public class FamilyMember
    {
        public bool Sex { get; private set; } // 性别
        public int Age { get; private set; } // 年龄
        public Family Family { get; set; } // 家庭
        public List<FamilyMember> Parent { get; } = new List<FamilyMember>(); // 父母
        public List<FamilyMember> Grandparents { get; } = new List<FamilyMember>(); // 祖父母
        public List<FamilyMember> Grandchildren { get; } = new List<FamilyMember>(); // 孙子女
        public FamilyMember Spouse { get; private set; } // 配偶
        public List<FamilyMember> Children { get; } = new List<FamilyMember>(); // 子女
        public List<FamilyMember> Siblings { get; } = new List<FamilyMember>(); // 兄弟姐妹
        public List<FamilyMember> Relatives { get; } = new List<FamilyMember>(); // 亲戚
        public List<FamilyMember> Friends { get; } = new List<FamilyMember>(); // 朋友
        public List<FamilyMember> Enemies { get; } = new List<FamilyMember>(); // 敌人
        public List<FamilyMember> Colleagues { get; } = new List<FamilyMember>(); // 同事


        public bool IsAdult
        {
            get
            {
                return Age >= 18;
            }
        }

        public Agent Agent { get; private set; } // 代理人
        public Job Job { get; set; } // 职业

        public FamilyMember(bool sex, int age)
        {
            Sex = sex;
            Age = age;
        }

        public void SetAgent(Agent agent)
        {
            Agent = agent;
        }

        private void SetParent(FamilyMember parent)
        {
            Parent.Add(parent);
        }

        public void SetSpouse(FamilyMember spouse)
        {
            Spouse = spouse;
            spouse.Spouse = this;
        }

        public void AddChild(FamilyMember child)
        {
            if (child != null)
            {
                Children.Add(child);
                child.SetParent(this);
            }
        }

        public void AddGrandchild(FamilyMember grandchild)
        {
            Grandchildren.Add(grandchild);
            grandchild.Grandparents.Add(this);
        }

        public void AddSibling(FamilyMember sibling)
        {
            Siblings.Add(sibling);
            sibling.Siblings.Add(this);
        }

        public void AddRelative(FamilyMember relative)
        {
            Relatives.Add(relative);
            relative.Relatives.Add(this);
        }

        public void AddFriend(FamilyMember friend)
        {
            Friends.Add(friend);
            friend.Friends.Add(this);
        }

        public void AddEnemy(FamilyMember enemy)
        {
            Enemies.Add(enemy);
            enemy.Enemies.Add(this);
        }

        public void SetJob(Job job)
        {
            if (Job != job)
            {
                Job?.Resign();
                Job = job;
            }
        }
    }
}