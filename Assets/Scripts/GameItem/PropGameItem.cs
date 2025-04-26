using System;
using System.Collections.Generic;
using AI;
using Citizens;
using UnityEngine;
using DG.Tweening;

namespace GameItem
{
    public class PropGameItem : GameItemBase<PropConfig>
    {
        public override bool Walkable => true;
        public int Count { get; private set; } = 1;

        private Tween _tween;

        private bool _isPicking;

        public PropGameItem(PropConfig config, Vector3 pos, int count) : base(config, pos)
        {
            Count = count;
        }

        public override void ShowUI()
        {
            base.ShowUI();
            UI.SetRenderer(Config.icon);
        }

        public override List<IAction> ItemActions()
        {
            return new List<IAction>()
            {
                new TakeItemInHand(this),
                new PutIntoBag(this),
            };
        }

        public void BePickedUp(Agent agent)
        {
            UI.Col.enabled = false; // Disable collider

            var originalPos = Pos;
            _tween = DOTween.To(() => Pos,
                                     v =>
                                     {
                                         Pos = v;
                                         if ((Pos - agent.Pos).sqrMagnitude < 0.5f && _tween != null)
                                         {
                                             if (agent.Bag.AddItem(this))
                                             {
                                                 DOTween.Kill(_tween);
                                                 _tween = null;
                                                 GameItemManager.DestroyGameItem(this);
                                             }
                                             else
                                             {
                                                 DOTween.Kill(_tween);
                                                _tween = null;
                                                 Pos = originalPos;
                                             }
                                         }
                                     },
                                     agent.Pos,
                                     0.25f)
                                 .SetEase(Ease.Linear);
                                //  .SetTarget(this);
        }
    }

    public class FoodItem : PropGameItem
    {
        public float FoodValue { get; set; } = 20;
        public int MaxFoodTimes { get; set; } = 5;
        public int FoodTimes { get; set; } = 5;

        public FoodItem(PropConfig config, Vector3 pos, int count) : base(config, pos, count)
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
        public BookItem(PropConfig config, Vector3 pos, int count) : base(config, pos, count)
        {
        }
    }

    public class PaperItem : PropGameItem
    {
        public PaperItem(PropConfig config, Vector3 pos, int count) : base(config, pos, count)
        {
        }
    }
}