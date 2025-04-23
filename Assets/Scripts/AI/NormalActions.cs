using System;
using System.Collections.Generic;
using Citizens;
using GameItem;
using Map;
using UnityEngine;

namespace AI
{
    // 抽象基类，封装前置动作执行逻辑
    public abstract class ActionBase : IAction
    {
        // 用于保存该动作执行前必须完成的动作
        public List<IAction> PrecedingActions { get; } = new List<IAction>();
        public virtual string ActionName { get; protected set; }
        public abstract float ProgressSpeed { get; protected set; }
        public abstract int ProgressTimes { get; protected set; }
        public virtual bool CanBeInterrupted => true;

        protected float CurProgress;
        protected int CurProgressStage = 0;

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

        protected void OnActionFailedEvent()
        {
            OnActionFailed?.Invoke(this);
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
        public void Execute(Agent agent)
        {
            ExecutePrecedingActions(agent);

            if (PrecedingActions.Count == 0 && !Done)
            {
                if (ProgressTimes != -1)
                {
                    if (CurProgress <= 100f)
                    {
                        float previousProgress = CurProgress;
                        CurProgress += GameManager.I.GameTime.DeltaTime * ProgressSpeed / 2f;
                        OnActionProgress?.Invoke(CurProgress);

                        // Debug.Log($"{ActionName} - {CurProgress}, {ProgressTimes}, {ProgressSpeed}");

                        int previousThreshold = (int)(previousProgress / (100f / ProgressTimes));
                        int currentThreshold = (int)(CurProgress / (100f / ProgressTimes));
                        int thresholdsPassed = currentThreshold - previousThreshold;

                        if (thresholdsPassed > 0)
                        {
                            CurProgressStage++;
                            DoExecute(agent);
                        }
                    }
                    else
                    {
                        Done = true;
                    }
                }
                else
                {
                    DoExecute(agent);
                }
            }
        }

        // 具体动作逻辑，由子类实现
        protected abstract void DoExecute(Agent agent);

        // 注册时配置前置动作（例如移动、捡取物品等）
        public abstract void OnRegister(Agent agent);

        public void Reset()
        {
            Done = false;
            PrecedingActions.Clear();
        }

        public virtual float Evaluate(Agent agent, HouseType houseType)
        {
            return 0f;
        }

        protected void CheckMoveToArroundPos(Agent agent, IGameItem item)
        {
            var pos = MapManager.I.GetItemArroundPos(agent, item);
            if (pos != Vector3.zero)
            {
                PrecedingActions.Add(new CheckMoveToTarget(pos));
            }
            else
            {
                Done = true;
            }
        }
    }

    // 检查是否需要移动到目标点的动作，如果当前位置不在目标附近，则执行移动
    public class CheckMoveToTarget : ActionBase
    {
        public override string ActionName => "Move to here";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; } = -1;

        private bool _isMoving = false;
        public Vector3 TargetPos { get; private set; }
        public CheckMoveToTarget(Vector3 targetPos, string targetName = "")
        {
            var cellPos = MapManager.I.WorldPosToCellPos(targetPos);
            TargetPos = new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f);
            ActionName = targetName;
        }

        public override void OnRegister(Agent agent)
        {
            // 不需要额外配置
        }

        protected override void DoExecute(Agent agent)
        {
            if (!_isMoving)
            {
                // 如果正在移动，则不执行任何操作
                if (!agent.MoveToTarget(TargetPos))
                {
                    Done = true;
                }
                _isMoving = true;
                return;
            }
            
            // 检查是否到达目标位置
            if (Vector3.Distance(agent.Pos, TargetPos) < 0.1f)
            {
                Done = true;
            }
        }
    }

    // 将物品放到指定位置（例如将食物放到嘴边或桌上）
    public class PutItemToTarget : ActionBase
    {
        public PutItemToTarget(PropGameItem item, GameItemBase targetItem)
        {
            throw new System.NotImplementedException();
        }

        public override string ActionName => "将物品放置到目标点";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

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
    public class TakeItemInHand : ActionBase
    {
        public override string ActionName => "捡取物品";
        public override float ProgressSpeed { get; protected set; } = 50;
        public override int ProgressTimes { get; protected set; } = 1;
        private PropGameItem _item;

        public TakeItemInHand(PropGameItem item)
        {
            _item = item;
        }

        public override void OnRegister(Agent agent)
        {
            // 此处可加入检查或移动动作
            if (Vector3.Distance(agent.Pos, _item.Pos) < 0.001f)
            {
                // TODO Do Take
                Done = true;
            }
            else
            {
                PrecedingActions.Add(new CheckMoveToTarget(_item.Pos));
            }
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行捡取物品动作");
            agent.TakeItemInHand(_item);
            GameManager.I.GameItemManager.RemoveGameItemOnMap(_item);
            // 模拟捡取物品的逻辑
            Done = true;
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
    public class EatAction : NormalAction
    {
        public override float ProgressSpeed { get; protected set; } = 5f;
        public override int ProgressTimes { get; protected set; }

        private FoodItem _foodItem;

        public EatAction(FoodItem foodItem, State state) : base(state)
        {
            _foodItem = foodItem;
            ActionName = "Eat";
        }

        public override void OnRegister(Agent agent)
        {
            // 检测附近最近的桌子（TODO: 替换为实际逻辑，例如选择空闲桌子或优先选择有其他NPC旁边的桌子）
            TableItem tableItem = agent.FindNearestTableItem();

            var takeItem = new TakeItemInHand(_foodItem);
            takeItem.OnRegister(agent);
            PrecedingActions.Add(takeItem);

            if (tableItem != null)
            {
                var putItemToTarget = new PutItemToTarget(_foodItem, tableItem);
                PrecedingActions.Add(putItemToTarget);
            }

            ProgressTimes = _foodItem.MaxFoodTimes;
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
    }

    // 示例行为：上厕所，需要先移动到厕所位置
    public class ToiletAction : NormalAction
    {
        public override string ActionName => "上厕所";
        public override float ProgressSpeed { get; protected set; } = 20f;
        public override int ProgressTimes { get; protected set; } = 1;

        private ToiletItem _toiletItem;
        public ToiletAction(State state) : base(state)
        {
            ActionName = "Toilet";
        }

        public override void OnRegister(Agent agent)
        {
            if (agent.Ciziten.Family.Houses[0].TryGetFurniture<ToiletItem>(out var toiletItem))
            {
                _toiletItem = toiletItem;
                PrecedingActions.Add(new CheckMoveToTarget(_toiletItem.Pos));
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
    }

    // 示例行为：社交，不需要额外移动
    public class SocialAction : NormalAction
    {
        public override string ActionName => "社交";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

        public SocialAction(State state) : base(state)
        {
            ActionName = "社交";
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
    }

    // 示例行为：游玩，需要先移动到游玩区域
    public class PlayAction : NormalAction
    {
        public override string ActionName => "游玩";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

        public PlayAction(State state) : base(state)
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
    }

    // 示例行为：洗澡，需要先移动到浴室位置
    public class BathAction : NormalAction
    {
        public override string ActionName => "洗澡";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

        private GameItemBase _bath;

        public BathAction(GameItemBase bath, State state) : base(state)
        {
            _bath = bath;
            ActionName = "洗澡";
        }

        public override void OnRegister(Agent agent)
        {
            // 加入前置动作：移动到浴室
            PrecedingActions.Add(new CheckMoveToTarget(_bath.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行洗澡动作，恢复清洁度");
            agent.State.Hygiene.Increase(100);
            Done = true;
        }
    }

    public class SleepAction : NormalAction
    {
        public override float ProgressSpeed { get => 100f / 8f / 60f / 60f; protected set {} }
        public override int ProgressTimes { get => 100; protected set {} }

        private BedItem _bedItem;
        public SleepAction(State state, BedItem bedItem = null) : base(state)
        {
            ActionName = "睡觉";
            _bedItem = bedItem;
        }

        public override void OnRegister(Agent agent)
        {
            if (_bedItem == null)
            {
                if (agent.Ciziten.Family.Houses[0].TryGetFurnitures<BedItem>(out var bedItems))
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

            PrecedingActions.Add(new CheckMoveToTarget(_bedItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            agent.State.Sleep.Increase(1);
        }
    }

    public class BackHome : ActionBase
    {
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

        public BackHome()
        {
            ActionName = "Back Home";
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

    public class SitAction : ActionBase
    {
        public override string ActionName => "Sit";
        public override float ProgressSpeed { get; protected set; } = 5f;
        public override int ProgressTimes { get; protected set; } = -1;

        private ChairItem _chairItem;

        public SitAction(ChairItem chairItem)
        {
            _chairItem = chairItem;
        }

        public override void OnRegister(Agent agent)
        {
            // 加入前置动作：移动到椅子位置
            PrecedingActions.Add(new CheckMoveToTarget(_chairItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行坐下动作");
            _chairItem.SitDown(agent);
            Done = true;
        }
    }

    public class ReadAction : ActionBase
    {
        public override string ActionName => "Read";
        public override float ProgressSpeed { get; protected set; } = 5f;
        public override int ProgressTimes { get; protected set; } = 1;

        private BookItem _bookItem;

        public ReadAction(BookItem bookItem)
        {
            _bookItem = bookItem;
        }

        public override void OnRegister(Agent agent)
        {
            // 加入前置动作：移动到书本位置
            PrecedingActions.Add(new CheckMoveToTarget(_bookItem.Pos));
            PrecedingActions.Add(new TakeItemInHand(_bookItem));
            PrecedingActions.Add(new SitAction(agent.GetGameItem<ChairItem>()));
        }

        protected override void DoExecute(Agent agent)
        {
            Debug.Log("执行阅读动作");
            Done = true;
        }
    }
}