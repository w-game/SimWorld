using System.Collections.Generic;
using AI;
using Citizens;
using UI.Elements;
using UI.Models;
using UnityEngine;
using UnityEngine.Events;

namespace GameItem
{
    public interface IProcessingItem : ISelectItem
    {
        PropConfig CurItem { get; }
        PropConfig ProduceItem { get; }
        void ProcessItem(int curTime, Agent agent);
        void OnTake(Agent agent);
        void Reset();
    }

    public abstract class ProcessingItemBase : FurnitureItem, IProcessingItem
    {
        public PropConfig CurItem { get; private set; }
        public PropConfig ProduceItem { get; private set; }

        public int ProduceAmount => System.Convert.ToInt32(CurItem.additionals[Amound]);
        public int ConvertionTime => System.Convert.ToInt32(CurItem.additionals[Time]);
        protected abstract string TargetId { get; }
        protected abstract string Amound { get; }
        protected abstract string Time { get; }
        protected abstract PropType PropType { get; }

        public event UnityAction<IProcessingItem> OnFinish;

        private ProducePanelElement _panel;
        protected ProcessingItemBase(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public virtual void OnSelected(string id, int amount = 1)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }
            CurItem = ConfigReader.GetConfig<PropConfig>(id);
            GameManager.I.CurrentAgent.Brain.RegisterAction(ActionPool.Get<ProcessingItemAction>(this, CurItem), true);
        }

        public virtual void ProcessItem(int curTime, Agent agent)
        {
            if (curTime >= ConvertionTime)
            {
                var targetId = ConfigReader.GetConfig<CropSeedConfig>(CurItem.id).targets[TargetId];
                ProduceItem = ConfigReader.GetConfig<PropConfig>(targetId);
                OnFinish?.Invoke(this);

                if (Owner == GameManager.I.CurrentAgent.Owner && ProduceItem != null)
                {
                    _panel = GameManager.I.GameItemManager.ItemUIPool.Get<ProducePanelElement>("Prefabs/UI/Elements/ProducePanelElement", Pos + new Vector3(0.5f, 1));
                    _panel.Init(ProduceItem, a => OnTake(agent), this);
                }
            }
        }

        public virtual void OnTake(Agent agent)
        {
            if (Vector3.Distance(agent.Pos, Pos) > 1f)
            {
                var moveAction = ActionPool.Get<CheckMoveToTarget>(agent, Pos + new Vector3(0.5f, -1f, 0));
                moveAction.NextAction = new SystemAction("Move to", a =>
                {
                    OnTake(agent);
                });

                agent.Brain.RegisterAction(moveAction, true);
                return;
            }

            if (CurItem != null && ProduceItem != null)
            {
                agent.Bag.AddItem(new PropItem(ProduceItem, ProduceAmount));
                Reset();
            }
        }

        public void Reset()
        {
            CurItem = null;
            ProduceItem = null;
            _panel?.ReleaseSelf();
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            if (CurItem != null && ProduceItem != null)
            {
                if (Owner == agent.Owner)
                {
                    return new List<IAction>()
                    {
                        new SystemAction("Take", a => OnTake(agent))
                    };
                }
                else
                {
                    return new List<IAction>()
                    {
                        new SystemAction("Steal", a => OnTake(agent))
                    };
                }
            }

            var actionName = TargetId.ToLower();
            return new List<IAction>()
            {
                new SystemAction(actionName, a =>
                {
                    var model = IModel.GetModel<PopSelectSeedModel>(this, PropType);
                    model.ShowUI();
                }),
            };
        }
    }

    public class MillstoneItem : ProcessingItemBase
    {
        public override bool Walkable => false;
        protected override string TargetId => "mill";

        protected override string Amound => "millAmount";

        protected override string Time => "millTime";
        protected override PropType PropType => PropType.Crop;

        public MillstoneItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>()
            {

            };
        }
    }

    public class FurnaceItem : ProcessingItemBase
    {
        public override bool Walkable => false;

        protected override string TargetId => "smelt";
        protected override string Amound => "smeltAmount";

        protected override string Time => "smeltTime";
        protected override PropType PropType => PropType.Ore;
        public FurnaceItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }
    }
}