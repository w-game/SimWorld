using System;
using System.Collections.Generic;
using Citizens;
using UnityEngine;

namespace AI
{
    public class AIController
    {
        private Agent _agent;
        private Queue<IAction> _activeActions;
        private IAction _curAction;

        // 每隔多少秒检测一次基础状态
        private float _evaluationInterval = 20.0f;
        private float _timeSinceLastEvaluation = 0f;
        public static event Action<IAction> OnActionRegister;

        public AIController(Agent agent)
        {
            _agent = agent;
            _activeActions = new Queue<IAction>();
        }

        public void RegisterAction(IAction action, bool force)
        {
            if (force && _activeActions.Count == 6)
            {
                List<IAction> tempList = new List<IAction>(_activeActions);
                tempList.RemoveAt(tempList.Count - 1);
                _activeActions = new Queue<IAction>(tempList);
            }

            action.OnRegister(_agent.State);
            _activeActions.Enqueue(action);
            OnActionRegister?.Invoke(action);
        }

        /// <summary>
        /// 结合状态效用和优先级修正来判断是否需要切换行为。
        /// 这里构造了多个候选行为，然后比较它们的综合效用，
        /// 如果某个候选行为的效用比当前正在执行的行为高出一定比例，则进行替换。
        /// </summary>
        private void CheckNormalState()
        {
            // 构建候选行为列表（可以根据需求增加更多行为）
            List<IAction> candidateActions = new List<IAction>
            {
                // new EatAction(),
                new ToiletAction(),
                new SleepAction(),
                new SocialAction()
            };

            IAction bestCandidate = null;
            float bestUtility = float.MinValue;

            foreach (IAction action in candidateActions)
            {
                float utility = action.CalculateUtility(_agent.State);
                Debug.Log($"行为 {action.ActionName} 的综合效用: {utility}");
                if (utility > bestUtility)
                {
                    bestUtility = utility;
                    bestCandidate = action;
                }
            }

            if (bestUtility > 20) return;

            RegisterAction(bestCandidate, true);
        }

        // 每个时间步更新状态，并根据评估周期进行行为检测
        public void Update()
        {
            _timeSinceLastEvaluation += Time.deltaTime;
            if (_timeSinceLastEvaluation >= _evaluationInterval)
            {
                CheckNormalState();
                _timeSinceLastEvaluation = 0f;
            }

            if (_curAction != null)
            {
                Log.LogInfo("AIController", "当前行为: " + _curAction.ActionName);
                _curAction.Execute(_agent.State);
            }
            else if (_activeActions.Count != 0)
            {
                _curAction = _activeActions.Dequeue();
                _curAction.OnCompleted += OnActionCompleted;
            }
        }
        
        private void OnActionCompleted(IAction action)
        {
            action.OnCompleted -= OnActionCompleted;
            _curAction = null;

            if (action.Done)
            {
                Log.LogInfo("AIController", "行为完成: " + action.ActionName);
            }
            else
            {
                Log.LogInfo("AIController", "行为未完成: " + action.ActionName);
                RegisterAction(action, true);
            }
        }
    }
}