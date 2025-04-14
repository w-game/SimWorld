using System.Collections.Generic;
using AI;
using Citizens;
using GameItem;
using UnityEngine;

namespace GameItem
{
    public class PropGameItem : StaticGameItem
    {
        public int Count { get; private set; } = 1;

        public Family Owner { get; private set; }

        public PropGameItem(ConfigBase config, int count, Vector3 pos = default) : base(config, pos)
        {
            Count = count;
        }

        public override List<IAction> ItemActions()
        {
            return new List<IAction>()
            {
                new TakeItemInHand(this),
                new PutIntoBag(this),
            };
        }
    }

    public class FoodItem : PropGameItem
    {
        public float FoodValue { get; set; } = 20;
        public int MaxFoodTimes { get; set; } = 5;
        public int FoodTimes { get; set; } = 5;

        public FoodItem(ConfigBase config, int count, Vector3 pos = default) : base(config, count, pos)
        {
        }

        internal void DecreaseFoodTimes()
        {
            FoodTimes--;
            if (FoodTimes <= 0)
            {
                Destroy();
            }
        }

        public override List<IAction> ItemActions()
        {
            return new List<IAction>()
            {
                new TakeItemInHand(this),
                new PutIntoBag(this),
                new EatAction(this, GameManager.I.CurrentAgent.State.Hunger),
            };
        }
    }

    public class BookItem : PropGameItem
    {
        public BookItem(ConfigBase config, int count, Vector3 pos = default) : base(config, count, pos)
        {
        }
    }

    public class PaperItem : PropGameItem
    {
        public PaperItem(ConfigBase config, int count, Vector3 pos = default) : base(config, count, pos)
        {
        }
    }
}