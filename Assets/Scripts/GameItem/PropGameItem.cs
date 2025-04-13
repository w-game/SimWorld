using System.Collections.Generic;
using AI;
using Citizens;
using GameItem;
using UnityEngine;

namespace GameItem
{
    public class PropGameItem : GameItemBase
    {
        public int Count { get; private set; } = 1;

        public Family Owner { get; private set; }

        public PropGameItem(ConfigBase config, int count, Vector3 pos = default) : base(config, pos)
        {
            Count = count;
        }

        public override List<IAction> OnSelected()
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
    }
}