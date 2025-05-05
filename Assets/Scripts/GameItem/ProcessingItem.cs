using System.Collections.Generic;
using AI;
using Citizens;
using Skill;
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
        void ProcessItem(int curTime);
        void OnTake(Agent agent);
        void Reset();
    }

    public abstract class ProcessingItemBase : FurnitureItem, IProcessingItem
    {
        public PropConfig CurItem { get; private set; }
        public PropConfig ProduceItem { get; private set; }

        public int ProduceAmount => System.Convert.ToInt32(CurItem.additionals[Amound]);
        public int ConvertionTime => System.Convert.ToInt32(CurItem.additionals[Time]);
        public float ConvertSpeed => System.Convert.ToSingle(CurItem.additionals[Speed]);
        protected abstract string TargetId { get; }
        protected abstract string Amound { get; }
        protected abstract string Time { get; }
        protected abstract string Speed { get; }
        public abstract PropType PropType { get; }

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

        public virtual void ProcessItem(int curTime)
        {
            if (curTime == ConvertionTime)
            {
                var targetId = CurItem.additionals[TargetId] as string;
                ProduceItem = ConfigReader.GetConfig<PropConfig>(targetId);
                OnFinish?.Invoke(this);

                if (Owner == GameManager.I.CurrentAgent.Owner && ProduceItem != null)
                {
                    _panel = GameManager.I.GameItemManager.ItemUIPool.Get<ProducePanelElement>("Prefabs/UI/Elements/ProducePanelElement", Pos + new Vector3(0.5f, 1));
                    _panel.Init(ProduceItem, a => OnTake(GameManager.I.CurrentAgent), this);
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

        protected virtual void TakeMaterial(Agent agent)
        {
            if (Vector3.Distance(agent.Pos, Pos) > 1f)
            {
                var moveAction = ActionPool.Get<CheckMoveToTarget>(agent, Pos + new Vector3(0.5f, -1f, 0));
                moveAction.NextAction = new SystemAction("Move to", a =>
                {
                    TakeMaterial(agent);
                });

                agent.Brain.RegisterAction(moveAction, true);
                return;
            }

            if (CurItem != null)
            {
                agent.Bag.AddItem(new PropItem(CurItem, ProduceAmount));
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
            } else if (CurItem != null)
            {
                return new List<IAction>()
                {
                    new SystemAction("Take", a => TakeMaterial(agent))
                };
            }

            

            return SkillAction();
        }

        protected virtual List<IAction> SkillAction()
        {
            var actionName = TargetId.ToLower();
            return new List<IAction>()
            {
                new SystemAction(actionName, a =>
                {
                    var model = IModel.GetModel<PopSelectSeedModel>(this);
                    model.ShowUI();
                })
            };
        }

        public virtual void ChangeItem(string id, int amount, Agent agent)
        {
            var config = ConfigReader.GetConfig<PropConfig>(id);
            agent.Bag.RemoveItem(config, amount);
            if (CurItem != null)
            {
                agent.Bag.AddItem(CurItem, amount);
            }

            CurItem = config;
        }
    }

    public class MillstoneItem : ProcessingItemBase
    {
        protected override string TargetId => "mill";

        protected override string Amound => "millAmount";

        protected override string Time => "millTime";

        public override PropType PropType => PropType.Crop;

        protected override string Speed => "millSpeed";

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
        protected override string TargetId => "smelt";
        protected override string Amound => "smeltAmount";
        protected override string Time => "smeltTime";
        public override PropType PropType => PropType.Material;
        protected override string Speed => "smeltSpeed";

        public FurnaceItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }
    }

    public class SeedIncubatorItem : ProcessingItemBase
    {
        protected override string TargetId => "seed";

        protected override string Amound => "seedAmount";
        protected override string Time => "seedTime";
        public override PropType PropType => PropType.Crop;
        protected override string Speed => "seedSpeed";

        private float _curProgress;
        private int _curTime;

        private ActionProgressElement _progress;

        public SeedIncubatorItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
        }

        protected override List<IAction> SkillAction()
        {
            var actionName = TargetId.ToLower();
            var valid = GameManager.I.CurrentAgent.CheckSkillLevel<PlantSkill>(3);
            var action = new SystemAction(GameManager.I.CurrentAgent.CheckSkillLevel<PlantSkill>(3) ? actionName : actionName + " (Plant Skill Lv.3)", a =>
                            {
                                var model = IModel.GetModel<PopSelectSeedModel>(this);
                                model.ShowUI();
                            });
            action.Enable = valid;
            return new List<IAction> { action };
        }

        public override void OnSelected(string id, int amount = 1)
        {
            if (Vector3.SqrMagnitude(GameManager.I.CurrentAgent.Pos - Pos) > 1f)
            {
                var list = ArroundPosList();
                if (list.Count == 0)
                {
                    return;
                }
                var moveAction = ActionPool.Get<CheckMoveToTarget>(GameManager.I.CurrentAgent, list[0]);
                moveAction.OnCompleted += (a, s) =>
                {
                    if (s)
                    {
                        OnSelected(id, amount);
                    }
                };

                GameManager.I.CurrentAgent.Brain.RegisterAction(moveAction, true);
                return;
            }


            ChangeItem(id, amount, GameManager.I.CurrentAgent);

            var cellPos = MapManager.I.WorldPosToCellPos(Pos);
            var targetPos = new Vector3(cellPos.x + 0.5f, cellPos.y - 0.2f);
            _progress = GameManager.I.GameItemManager.ItemUIPool.Get<ActionProgressElement>("Prefabs/ActionProgress", targetPos);
        }

        public override void Update()
        {
            if (CurItem == null) return;
            _curProgress += GameTime.DeltaTime * ConvertSpeed;
            if (_curProgress >= 100f)
            {
                _curTime++;
                _curProgress = 0;
                ProcessItem(_curTime);
                if (_curTime == ConvertionTime)
                {
                    _progress?.ReleaseSelf(null, true);
                    _progress = null;
                }
            }
            _progress?.SetProgress(_curProgress);
        }
    }
}