using System;
using System.Collections.Generic;
using Citizens;
using GameItem;
using Map;
using UI.Elements;
using UnityEngine;
using UnityEngine.Events;

namespace AI
{
    // 抽象基类，封装前置动作执行逻辑
    public abstract class ActionBase : IAction, IActionPool
    {
        // 用于保存该动作执行前必须完成的动作
        public List<IAction> PrecedingActions { get; } = new List<IAction>();
        public virtual string ActionName { get; protected set; }
        public virtual bool CanBeInterrupted => true;

        public Vector3 Target
        {
            set
            {
                var cellPos = MapManager.I.WorldPosToCellPos(value);
                var targetPos = new Vector3(cellPos.x + 0.5f, cellPos.y - 0.2f);
                var actionProgressElement = GameManager.I.GameItemManager.ItemUIPool.Get<ActionProgressElement>("Prefabs/ActionProgress", targetPos);
                actionProgressElement.Init(this, null);
            }
        }

        private bool _done = false;

        public bool Done
        {
            get => _done;
            set
            {
                _done = value;
                if (_done)
                {
                    OnCompleted?.Invoke(this);
                }
            }
        }

        public event Action<float> OnActionProgress;
        public event Action<IAction> OnCompleted;
        public event Action<IAction> OnActionFailed;

        // 后续行为，当前行为完成后替换主行为
        public IAction NextAction { get; set; }

        public bool Enable { get; set; } = true;
        public bool Pause { get; set; } = false;

        protected void OnActionFailedEvent()
        {
            OnActionFailed?.Invoke(this);
        }

        protected void OnActionProgressEvent(float progress)
        {
            OnActionProgress?.Invoke(progress);
        }

        // 先执行前置动作（如果有的话）
        public void ExecutePrecedingActions(Agent agent)
        {
            if (PrecedingActions.Count > 0)
            {
                var action = PrecedingActions[0];
                if (action.Done)
                {
                    PrecedingActions.RemoveAt(0);
                }
                else
                {
                    action.Execute(agent);
                }
            }
        }

        // 执行流程：先执行前置动作，前置动作完成后再执行本动作
        public virtual void Execute(Agent agent) { }

        // 具体动作逻辑，由子类实现
        protected abstract void DoExecute(Agent agent);

        // 注册时配置前置动作（例如移动、捡取物品等）
        public abstract void OnRegister(Agent agent);

        public void Reset()
        {
            Done = false;
            PrecedingActions.Clear();
            NextAction = null;
            Enable = true;
            Pause = false;

            OnActionFailed = null;
            OnCompleted = null;
            OnActionProgress = null;
        }

        public virtual float Evaluate(Agent agent, HouseType houseType)
        {
            return 0f;
        }

        protected void CheckMoveToArroundPos(Agent agent, Vector3 targetPos, UnityAction onComplete = null)
        {
            var pos = MapManager.I.GetItemArroundPos(agent, targetPos);
            if (pos != Vector3.zero)
            {
                var action = ActionPool.Get<CheckMoveToTarget>(agent, pos);
                action.OnCompleted += (a) => onComplete?.Invoke();
                PrecedingActions.Add(action);
            }
            else
            {
                Done = true;
            }
        }

        public abstract void OnGet(params object[] args);

        public virtual void OnRelease()
        {
            Reset();
        }
    }

    public abstract class SingleActionBase : ActionBase
    {
        protected float ActionSpeed { get; set; } = 100f;
        private float _curProgress;

        public override void OnRegister(Agent agent)
        {
            // 不需要额外配置
        }

        public override void Execute(Agent agent)
        {
            ExecutePrecedingActions(agent);

            if (Done || PrecedingActions.Count != 0 || Pause) return;

            if (_curProgress < 100f)
            {
                _curProgress += GameTime.DeltaTime * ActionSpeed;
                OnActionProgressEvent(_curProgress);
            }
            else
            {
                DoExecute(agent);
                Done = true;
            }
        }
    }

    public abstract class MultiTimesActionBase : ActionBase
    {
        protected int TotalTimes { get; set; }
        protected float ProgressSpeed { get; set; }
        private int _curTimes = 0;
        private float _curProgress;

        public override void Execute(Agent agent)
        {
            ExecutePrecedingActions(agent);

            if (Done || PrecedingActions.Count != 0 || Pause) return;

            if (_curProgress < 100f)
            {
                _curProgress += GameTime.DeltaTime * ProgressSpeed / 2f;
                OnActionProgressEvent(_curProgress);
            }
            else
            {
                DoExecute(agent);
                _curProgress = 0f;
                _curTimes++;
                if (_curTimes >= TotalTimes)
                {
                    Done = true;
                }
            }
        }
    }

    public abstract class ConditionActionBase : ActionBase
    {
        protected Func<bool> Condition { get; set; }

        public override void Execute(Agent agent)
        {
            ExecutePrecedingActions(agent);

            if (Done || PrecedingActions.Count != 0 || Pause) return;

            if (Condition != null && Condition.Invoke())
            {
                Done = true;
            }
            else
            {
                DoExecute(agent);
            }
        }
    }

    // 检查是否需要移动到目标点的动作，如果当前位置不在目标附近，则执行移动
    public class CheckMoveToTarget : ConditionActionBase
    {
        public override string ActionName => "Move to here";

        public Vector3 TargetPos { get; private set; }

        private bool _isMoving = false;

        public override void OnRegister(Agent agent)
        {
        }

        protected override void DoExecute(Agent agent)
        {
            if (_isMoving)
            {
                agent.MoveToTarget();
            }
            else
            {
                _isMoving = true;
                agent.MoveToTarget(TargetPos);
            }
        }

        public override void OnGet(params object[] args)
        {
            var agent = args[0] as Agent;
            var targetPos = (Vector3)args[1];
            var cellPos = MapManager.I.WorldPosToCellPos(targetPos);
            TargetPos = new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f);
            ActionName = "Move to";
            Condition = () => _isMoving && agent.CheckArriveTargetPos();
            _isMoving = false;
        }
    }

    // 将物品放到指定位置（例如将食物放到嘴边或桌上）
    public class PutItemToTarget : SingleActionBase
    {
        public override string ActionName => "将物品放置到目标点";

        public override void OnGet(params object[] args)
        {
            throw new NotImplementedException();
        }

        public override void OnRegister(Agent agent)
        {
            // 可在此添加需要的前置动作（如检查是否已移动到目标）
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行将物品放置到目标点动作");
            // 模拟放置物品的逻辑
            Done = true;
        }
    }

    // 捡取物品动作（例如从地上捡取食物）
    public class TakeItemInHand : SingleActionBase
    {
        public override string ActionName => "捡取物品";
        private PropGameItem _item;

        public override void OnRegister(Agent agent)
        {
            if (Vector3.Distance(agent.Pos, _item.Pos) > 0.5f)
            {
                PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _item.Pos));
            }
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行捡取物品动作");
            agent.TakeItemInHand(_item);
            ActionSpeed = 0f;
        }

        public override void OnGet(params object[] args)
        {
            _item = args[0] as PropGameItem;
        }
    }

    public abstract class NormalAction : ActionBase
    {
        public State State { get; private set; }
        public NormalAction(State state)
        {
            State = state;
        }
    }

    // 示例行为：吃东西，根据饥饿值决定效用
    public class EatAction : MultiTimesActionBase
    {
        private FoodItem _foodItem;

        public override void OnRegister(Agent agent)
        {
            // 检测附近最近的桌子（TODO: 替换为实际逻辑，例如选择空闲桌子或优先选择有其他NPC旁边的桌子）
            TableItem tableItem = agent.FindNearestTableItem();

            var takeItem = ActionPool.Get<TakeItemInHand>(_foodItem);
            takeItem.OnRegister(agent);
            PrecedingActions.Add(takeItem);

            if (tableItem != null)
            {
                var putItemToTarget = ActionPool.Get<PutItemToTarget>(_foodItem, tableItem);
                PrecedingActions.Add(putItemToTarget);
            }
        }

        protected override void DoExecute(Agent agent)
        {
            // 每个阈值增加的饱食度：使用 FoodValue * ProgressSpeed / 100 的计算公式
            float increment = _foodItem.FoodValue / _foodItem.MaxFoodTimes;
            agent.State.Hunger.Increase(increment);
            _foodItem.DecreaseFoodTimes();
            Debug.Log($"饱食度增加了 {increment}，当前饱食度: {agent.State.Hunger}");
        }

        public override float Evaluate(Agent agent, HouseType houseType)
        {
            return base.Evaluate(agent, houseType);
        }

        public override void OnGet(params object[] args)
        {
            _foodItem = args[0] as FoodItem;
            ActionName = "Eat";

            ProgressSpeed = _foodItem.MaxFoodTimes;
            TotalTimes = 5;
        }
    }

    // 示例行为：上厕所，需要先移动到厕所位置
    public class ToiletAction : SingleActionBase
    {
        public override string ActionName => "上厕所";

        private ToiletItem _toiletItem;

        public override void OnRegister(Agent agent)
        {
            if (agent.Citizen.Family.Houses.Count == 0)
            {
                Debug.LogError("没有房子，无法上厕所");
                Done = true;
                return;
            }

            if (agent.Citizen.Family.Houses[0].TryGetFurniture<ToiletItem>(out var toiletItem))
            {
                _toiletItem = toiletItem;
                PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _toiletItem.Pos));
            }
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行上厕所动作，恢复厕所值");
            if (_toiletItem == null)
            {
                if (agent.State.Toilet.Value <= 0f)
                {
                    agent.State.Toilet.Increase(100);
                }
                Done = true;
                return;
            }
            else
            {
                Debug.Log("执行上厕所动作，恢复厕所值");
                agent.State.Toilet.Increase(100);
                Done = true;
            }
        }

        public override void OnGet(params object[] args)
        {
            ActionName = "Toilet";
            ActionSpeed = 10f;
        }
    }

    // 示例行为：社交，不需要额外移动
    public class SocialAction : SingleActionBase
    {
        public override string ActionName => "社交";

        public SocialAction() : base()
        {
            
        }

        public override void OnRegister(Agent agent)
        {
            // 无前置动作
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行社交动作，提升社交值");
            agent.State.Social.Increase(10);
            Done = true;
        }

        public override void OnGet(params object[] args)
        {
            ActionName = "社交";
        }
    }

    // 示例行为：游玩，需要先移动到游玩区域
    public class PlayAction : SingleActionBase
    {
        public override string ActionName => "游玩";

        public PlayAction() : base()
        {
        }

        public override void OnRegister(Agent agent)
        {
            // 加入前置动作：移动到游玩区域
            // PrecedingActions.Add(new CheckMoveToTarget(playPos));
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行游玩动作，提升心情");
            agent.State.Mood.Increase(10);
            Done = true;
        }

        public override void OnGet(params object[] args)
        {
            throw new NotImplementedException();
        }
    }

    // 示例行为：洗澡，需要先移动到浴室位置
    public class BathAction : SingleActionBase
    {
        public override string ActionName => "洗澡";

        private IGameItem _bath;

        public BathAction() : base()
        {
        }

        public override void OnRegister(Agent agent)
        {
            // 加入前置动作：移动到浴室
            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _bath.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行洗澡动作，恢复清洁度");
            agent.State.Hygiene.Increase(100);
            Done = true;
        }

        public override void OnGet(params object[] args)
        {
            throw new NotImplementedException();
        }
    }

    public class SleepAction : MultiTimesActionBase
    {
        private BedItem _bedItem;

        public override void OnGet(params object[] args)
        {
            ActionName = "Sleep";
            _bedItem = args[0] as BedItem;

            ProgressSpeed = 100f;
            TotalTimes = 5;
        }

        public override void OnRegister(Agent agent)
        {
            if (_bedItem == null)
            {
                if (agent.Citizen.Family.Houses[0].TryGetFurnitures<BedItem>(out var bedItems))
                {
                    foreach (var bedItem in bedItems)
                    {
                        if (bedItem.Using == null)
                        {
                            _bedItem = bedItem;
                            _bedItem.Using = agent;
                            break;
                        }
                    }
                }
            }

            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _bedItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            agent.State.Sleep.Increase(1);
        }
    }

    public class BackHome : ConditionActionBase
    {
        public BackHome()
        {
            ActionName = "Back Home";
        }

        public override void OnGet(params object[] args)
        {
            throw new NotImplementedException();
        }

        public override void OnRegister(Agent agent)
        {
            // PrecedingActions.Add(new CheckMoveToTarget(agent.HomePos));
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行回家动作");
            Done = true;
        }
    }

    public class SitAction : SingleActionBase
    {
        public override string ActionName => "Sit";
        private ChairItem _chairItem;

        public SitAction()
        {
        }

        public override void OnRegister(Agent agent)
        {
            // 加入前置动作：移动到椅子位置
            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _chairItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行坐下动作");
            _chairItem.SitDown(agent);
        }

        public override void OnGet(params object[] args)
        {
            _chairItem = args[0] as ChairItem;
            ActionName = "Sit";
        }
    }

    public class ReadAction : MultiTimesActionBase
    {
        public override string ActionName => "Read";

        private BookItem _bookItem;

        public override void OnRegister(Agent agent)
        {
            // 加入前置动作：移动到书本位置
            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _bookItem.Pos));
            PrecedingActions.Add(ActionPool.Get<TakeItemInHand>(_bookItem));
            PrecedingActions.Add(ActionPool.Get<SitAction>(agent.GetGameItem<ChairItem>()));
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行阅读动作");
        }

        public override void OnGet(params object[] args)
        {
            _bookItem = args[0] as BookItem;
            ActionName = "Read";

            ProgressSpeed = 10f;
            TotalTimes = 5;
        }
    }
}