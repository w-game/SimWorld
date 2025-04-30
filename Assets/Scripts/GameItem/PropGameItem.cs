using System;
using System.Collections.Generic;
using AI;
using Citizens;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

namespace GameItem
{
    public class PropGameItem : GameItemBase<PropConfig>
    {
        public override bool Walkable => true;
        public int Count { get; private set; } = 1;

        private Agent _agent;
        private bool _isPickedUp = false;
        protected event UnityAction<bool> OnTakedEvent;
        public PropItem PropItem { get; private set; }

        public PropGameItem(PropConfig config, Vector3 pos, int count) : base(config, pos)
        {
            Count = count;
            PropItem = new PropItem(config, count);
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
                ActionPool.Get<PutIntoBag>(this, agent is Agent a && Owner != a.Owner)
            };
        }

        public void CheckPickUp(Agent agent)
        {
            if (Owner != agent.Citizen.Family || _isPickedUp)
                return;

            _isPickedUp = true;

            BePickedUp(agent);
        }

        public void BePickedUp(Agent agent)
        {
            DOTween.Sequence()
                .AppendInterval(0.1f)
                .AppendCallback(() =>
                {
                    if (agent.Bag.AddItem(PropItem))
                    {
                        _agent = agent;
                        OnTakedEvent?.Invoke(agent.Owner != Owner);
                        GameItemManager.DestroyGameItem(this);
                    }
                });
        }

        public override void Destroy()
        {
            if (_agent != null)
            {
                UI.PlayAnimation(_agent, () =>
                {
                    base.Destroy();
                });
            }
            else
            {
                base.Destroy();
            }
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

    public class SellItem : PropGameItem
    {
        private ShopShelfItem _shopShelfItem;
        public SellItem(PropConfig config, Vector3 pos, int count, ShopShelfItem shopShelfItem) : base(config, pos, count)
        {
            OnTakedEvent += OnTaked;
            _shopShelfItem = shopShelfItem;
        }

        private void OnTaked(bool isSteal)
        {
            _shopShelfItem.OnTaked(isSteal);
        }

        public override void ShowUI()
        {
            base.ShowUI();
            UI.Col.enabled = false;
        }
    }
}