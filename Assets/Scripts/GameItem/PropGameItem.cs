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
        private Agent _agent;
        private bool _isPickedUp = false;
        protected event UnityAction<bool> OnTakedEvent;
        public PropItem PropItem { get; private set; }

        public PropGameItem(PropConfig config, Vector3 pos, int count) : base(config, pos)
        {
            PropItem = new PropItem(config, count);
            Walkable = true;
        }

        public override void ShowUI()
        {
            base.ShowUI();
            UI.SetRenderer(Config.icon);
            UI.SetName(Config.name);
        }

        public virtual void AddCount(int count)
        {
            PropItem.AddQuantity(count);
            if (PropItem.Quantity <= 0)
            {
                GameItemManager.DestroyGameItem(this);
            }
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>()
            {
                // ActionPool.Get<TakeItemInHand>(this),
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
        public int FoodTimes { get; set; } = 5;

        public FoodItem(PropConfig config, Vector3 pos, int count) : base(config, pos, count)
        {
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            var actions = base.ItemActions(agent);
            if (agent is Agent a && Owner == a.Citizen.Family)
                actions.Add(ActionPool.Get<EatAction>(this, GameManager.I.CurrentAgent.State.Hunger));
            return actions;
        }

        public override void AddCount(int count)
        {
            base.AddCount(count);
            if (PropItem.Quantity > 0)
            {
                FoodValue = 20;
                FoodTimes = 5;
            }
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