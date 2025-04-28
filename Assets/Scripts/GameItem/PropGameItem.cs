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
            UI.SetName(Config.name);
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>()
            {
                ActionPool.Get<TakeItemInHand>(this),
                agent is Agent a && Owner == a.Citizen.Family ? ActionPool.Get<PutIntoBag>(this) : ActionPool.Get<StealAction>(this),
            };
        }

        public void BePickedUp(Agent agent)
        {
            if (Owner != agent.Citizen.Family)
                return;

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

        public override List<IAction> ItemActions(IGameItem agent)
        {
            var actions = base.ItemActions(agent);
            if (agent is Agent a && Owner == a.Citizen.Family)
                actions.Add(ActionPool.Get<EatAction>(this, GameManager.I.CurrentAgent.State.Hunger));
            return actions;
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