using System;
using System.Collections.Generic;
using Citizens;
using GameItem;
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

        protected float CurProgress;

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
        
        public static event Action<float> OnProgress;
        public event Action<IAction> OnCompleted;

        public abstract float CalculateUtility(AgentState state);

        // 先执行前置动作（如果有的话）
        public void ExecutePrecedingActions(AgentState state)
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
                    action.Execute(state);
                }
            }
        }

        // 执行流程：先执行前置动作，前置动作完成后再执行本动作
        public void Execute(AgentState state)
        {
            ExecutePrecedingActions(state);

            if (PrecedingActions.Count == 0 && !Done)
            {
                if (ProgressTimes != -1)
                {
                    if (CurProgress < 100f)
                    {
                        float previousProgress = CurProgress;
                        CurProgress += Time.deltaTime * ProgressSpeed;
                        OnProgress?.Invoke(CurProgress);

                        // Debug.Log($"{ActionName} - {CurProgress}, {ProgressTimes}, {ProgressSpeed}");

                        int previousThreshold = (int)(previousProgress / (100f / ProgressTimes));
                        int currentThreshold = (int)(CurProgress / (100f / ProgressTimes));
                        int thresholdsPassed = currentThreshold - previousThreshold;

                        if (thresholdsPassed > 0)
                        {
                            DoExecute(state);
                        }
                    }
                    else
                    {
                        Done = true;
                    }
                }
                else
                {
                    DoExecute(state);
                    Done = true;
                }
            }
        }

        // 具体动作逻辑，由子类实现
        protected abstract void DoExecute(AgentState state);

        // 注册时配置前置动作（例如移动、捡取物品等）
        public abstract void OnRegister(AgentState state);
    }

    // 检查是否需要移动到目标点的动作，如果当前位置不在目标附近，则执行移动
    public class CheckMoveToTarget : ActionBase
    {
        public Vector3 TargetPos { get; private set; }
        public override string ActionName => "Move to here";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; } = -1;
        private bool _isMoving = false;

        public CheckMoveToTarget(Vector3 targetPos)
        {
            TargetPos = targetPos;
        }
        
        public override float CalculateUtility(AgentState state)
        {
            // 此动作仅作为前置动作使用，不参与决策
            return 0f;
        }

        public override void OnRegister(AgentState state)
        {
            // 不需要额外配置
        }

        protected override void DoExecute(AgentState state)
        {
            if (Vector3.Distance(state.Pos, TargetPos) > 0.1f)
            {
                if (_isMoving) return;
                Debug.Log($"【移动系统】从 {state.Pos} 移动到 {TargetPos}");
                state.Agent.MoveToTarget(TargetPos);
                _isMoving = true;
            }
            else
            {
                Done = true;
            }
        }
    }

    // 将物品放到指定位置（例如将食物放到嘴边或桌上）
    public class PutItemToTarget : ActionBase
    {
        public PutItemToTarget(GameItemBase item, GameItemBase targetItem)
        {
            throw new System.NotImplementedException();
        }
        
        public override string ActionName => "将物品放置到目标点";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

        public override float CalculateUtility(AgentState state)
        {
            // 此动作仅作为前置动作使用，不参与效用决策
            return 0f;
        }

        public override void OnRegister(AgentState state)
        {
            // 可在此添加需要的前置动作（如检查是否已移动到目标）
        }

        protected override void DoExecute(AgentState state)
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
        private Agent _agent;
        private GameItemBase _item;

        public TakeItemInHand(Agent agent, GameItemBase item)
        {
            _agent = agent;
            _item = item;
        }

        public override float CalculateUtility(AgentState state)
        {
            // 此动作仅作为前置动作使用
            return 0f;
        }

        public override void OnRegister(AgentState state)
        {
            // 此处可加入检查或移动动作
            if (Vector3.Distance(_agent.transform.position, _item.transform.position) < 0.001f)
            {
                // TODO Do Take
                Done = true;
            }
            else
            {
                PrecedingActions.Add(new CheckMoveToTarget(_item.transform.position));
            }
        }

        protected override void DoExecute(AgentState state)
        {
            Debug.Log("执行捡取物品动作");
            state.Agent.TakeItemInHand(_item);
            MapManager.I.RemoveGameItemOnMap(_item);
            // 模拟捡取物品的逻辑
            Done = true;
        }
    }

    // 示例行为：吃东西，根据饥饿值决定效用
    public class EatAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 5f;
        public override int ProgressTimes { get; protected set; }

        private FoodItem _foodItem;

        public EatAction(FoodItem foodItem)
        {
            _foodItem = foodItem;
            ActionName = "Eat";
        }

        public override float CalculateUtility(AgentState state)
        {
            return 100 - state.Hunger;
        }

        public override void OnRegister(AgentState state)
        {
            // 检测附近最近的桌子（TODO: 替换为实际逻辑，例如选择空闲桌子或优先选择有其他NPC旁边的桌子）
            GameItemBase tableItem = state.Agent.FindNearestTableItem();

            var takeItem = new TakeItemInHand(state.Agent, _foodItem);
            takeItem.OnRegister(state);
            PrecedingActions.Add(takeItem);

            if (tableItem != null)
            {
                var putItemToTarget = new PutItemToTarget(_foodItem, tableItem);
                PrecedingActions.Add(putItemToTarget);
            }

            ProgressTimes = _foodItem.MaxFoodTimes;
        }

        protected override void DoExecute(AgentState state)
        {
            // 每个阈值增加的饱食度：使用 FoodValue * ProgressSpeed / 100 的计算公式
            float increment = _foodItem.FoodValue / _foodItem.MaxFoodTimes;
            state.Hunger = Mathf.Min(state.Hunger + increment, 100);
            _foodItem.DecreaseFoodTimes();
            Debug.Log($"饱食度增加了 {increment}，当前饱食度: {state.Hunger}");
        }
    }

    // 示例行为：上厕所，需要先移动到厕所位置
    public class ToiletAction : ActionBase
    {
        public override string ActionName => "上厕所";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

        private Vector3 _targetPos;

        public override float CalculateUtility(AgentState state)
        {
            return 100 - state.Toilet;
        }

        public override void OnRegister(AgentState state)
        {
            // 加入前置动作：移动到厕所位置
            PrecedingActions.Add(new CheckMoveToTarget(_targetPos));
        }

        protected override void DoExecute(AgentState state)
        {
            Debug.Log("执行上厕所动作，恢复厕所值");
            state.Toilet = 100;
            Done = true;
        }
    }

    // 示例行为：社交，不需要额外移动
    public class SocialAction : ActionBase
    {
        public override string ActionName => "社交";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

        public override float CalculateUtility(AgentState state)
        {
            return 100 - state.Social;
        }

        public override void OnRegister(AgentState state)
        {
            // 无前置动作
        }

        protected override void DoExecute(AgentState state)
        {
            Debug.Log("执行社交动作，提升社交值");
            state.Social = 100;
            Done = true;
        }
    }

    // 示例行为：游玩，需要先移动到游玩区域
    public class PlayAction : ActionBase
    {
        public override string ActionName => "游玩";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

        private Vector3 playPos;

        public PlayAction(Vector3 playPos)
        {
            this.playPos = playPos;
        }

        public override float CalculateUtility(AgentState state)
        {
            return 100 - state.Mood;
        }

        public override void OnRegister(AgentState state)
        {
            // 加入前置动作：移动到游玩区域
            PrecedingActions.Add(new CheckMoveToTarget(playPos));
        }

        protected override void DoExecute(AgentState state)
        {
            Debug.Log("执行游玩动作，提升心情");
            state.Mood = 100;
            Done = true;
        }
    }

    // 示例行为：洗澡，需要先移动到浴室位置
    public class BathAction : ActionBase
    {
        public override string ActionName => "洗澡";
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

        private Vector3 bathPos;

        public BathAction(Vector3 bathPos)
        {
            this.bathPos = bathPos;
        }

        public override float CalculateUtility(AgentState state)
        {
            return 100 - state.Hygiene;
        }

        public override void OnRegister(AgentState state)
        {
            // 加入前置动作：移动到浴室
            PrecedingActions.Add(new CheckMoveToTarget(bathPos));
        }

        protected override void DoExecute(AgentState state)
        {
            Debug.Log("执行洗澡动作，恢复清洁度");
            state.Hygiene = 100;
            Done = true;
        }
    }

    public class SleepAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; }
        public override int ProgressTimes { get; protected set; }

        public override float CalculateUtility(AgentState state)
        {
            return 100 - state.Sleep;
        }

        protected override void DoExecute(AgentState state)
        {
            throw new System.NotImplementedException();
        }

        public override void OnRegister(AgentState state)
        {
            throw new System.NotImplementedException();
        }
    }
}