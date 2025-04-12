using System;
using System.Collections.Generic;
using Citizens;
using GameItem;
using UnityEngine;

namespace AI
{
    public class AIController
    {
        private Agent _agent;
        private Queue<IAction> _activeActionsFirst;
        private Queue<IAction> _activeActionsSecond;
        private Queue<IAction> _activeActionsThird;

        private IAction _curAction;

        public event Action<IAction> OnActionRegister;

        private Dictionary<State, float> _actionCooldowns = new();
        private float _actionCooldownTime = 10f;

        public event Action<float> OnActionProgress;

        public AIController()
        {
            _activeActionsFirst = new Queue<IAction>();
        }

        public void SetAgent(Agent agent)
        {
            _agent = agent;
        }

        internal void AddDetector(IActionDetector actionDetector)
        {

        }

        public void RegisterAction(IAction action, bool force)
        {
            if (force && _activeActionsFirst.Count == 6)
            {
                List<IAction> tempList = new List<IAction>(_activeActionsFirst);
                tempList.RemoveAt(tempList.Count - 1);
                _activeActionsFirst = new Queue<IAction>(tempList);
            }

            action.OnRegister(_agent);
            _activeActionsFirst.Enqueue(action);
            OnActionRegister?.Invoke(action);
        }

        /// <summary>
        /// 结合状态效用和优先级修正来判断是否需要切换行为。
        /// 这里构造了多个候选行为，然后比较它们的综合效用，
        /// 如果某个候选行为的效用比当前正在执行的行为高出一定比例，则进行替换。
        /// </summary>
        private bool CheckNormalState()
        {
            if (_curAction != null && !_curAction.CanBeInterrupted)
                return false;

            List<State> states = new List<State>
            {
                _agent.State.Health,
                _agent.State.Hunger,
                _agent.State.Toilet,
                _agent.State.Social,
                _agent.State.Mood,
                _agent.State.Sleep,
                _agent.State.Hygiene
            };

            float bestUtility = float.MinValue;
            State bestState = null;

            foreach (State state in states)
            {
                float utility = state.CheckState(_agent.State.Mood.Value);
                if (utility > bestUtility)
                {
                    if (_actionCooldowns.TryGetValue(state, out float lastTime))
                    {
                        if (Time.time - lastTime < _actionCooldownTime)
                            continue; // 冷却中，不执行
                    }
                    bestUtility = utility;
                    bestState = state;
                }
            }

            if (bestUtility < 100f)
                return false; // 当前没有足够的效用驱动切换行为

            _actionCooldowns[bestState] = Time.time;

            if (_curAction is NormalAction curNormalAction)
            {
                float currentUtility = curNormalAction.State.CheckState(_agent.State.Mood.Value);
                Debug.Log($"当前行为效用: {currentUtility}, 新行为效用: {bestUtility}");
                if (bestUtility < currentUtility * 1.2f) // 只有新行为效用明显高，才换
                    return false;
            }

            else if (_curAction is WorkAction curWorkAction)
            {
                if (bestState.Name == "Social"
                || bestState.Name == "Mood"
                || bestState.Name == "Sleep"
                || bestState.Name == "Hygiene")
                    return false; // 当前行为是工作，且新状态不是社交或心情，则不换
            }

            Debug.Log($"当前行为: {_curAction?.ActionName}, 新行为: {bestState.Name}, 效用: {bestUtility}");

            switch (bestState.Name)
            {
                case "Health":
                    break;
                case "Hunger":
                    var foodItem = _agent.GetGameItem<FoodItem>();
                    if (foodItem != null)
                    {
                        RegisterAction(new EatAction(foodItem, _agent.State.Hunger), true);
                    }
                    else
                    {
                        var stoveItem = _agent.GetGameItem<StoveItem>();
                        if (stoveItem != null)
                        {
                            Debug.Log($"StoveItem: {stoveItem.Pos}");
                            RegisterAction(new CookAction(stoveItem), true);
                        }
                    }
                    break;
                case "Toilet":
                    var toiletItem = _agent.GetGameItem<ToiletItem>();
                    if (toiletItem != null)
                        RegisterAction(new ToiletAction(toiletItem, _agent.State.Toilet), true);
                    else
                    {
                        if (_agent.State.Toilet.Value <= 0f)
                        {
                            RegisterAction(new ToiletAction(null, _agent.State.Toilet), true);
                        }
                    }
                    break;
                case "Social":
                    // RegisterAction(new SocialAction(_agent.State.Social), true);
                    break;
                case "Mood":
                    // RegisterAction(new PlayAction(_agent.State.Mood), true);
                    break;
                case "Sleep":
                    var bedItem = _agent.GetGameItem<BedItem>();
                    if (bedItem != null)
                        RegisterAction(new SleepAction(bedItem, _agent.State.Sleep), true);
                    else
                        // 如果没有床，直接睡觉
                        // 这里可以考虑添加一个新的行为，比如在地上睡觉
                        // RegisterAction(new SleepAction(null, _agent.State.Sleep), true);
                        break;
                    break;
                case "Hygiene":
                    // RegisterAction(new BathAction(null, _agent.State.Hygiene), true);
                    break;
            }

            return true;
        }

        // 每个时间步更新状态，并根据评估周期进行行为检测
        public void Update()
        {
            var result = CheckNormalState();

            if (_curAction != null)
            {
                Log.LogInfo("AIController", "当前行为: " + _curAction.ActionName);
                _curAction.Execute(_agent);
            }
            else if (_activeActionsFirst.Count != 0)
            {
                _curAction = _activeActionsFirst.Dequeue();
                _curAction.OnCompleted += OnActionCompleted;
                _curAction.OnActionProgress += OnActionProgress;
            }
            else if (!result)
            {
                // TODO: 发呆、按照兴趣指派行为等
                if (GameManager.I.CurrentAgent != _agent)
                {
                    var prob = UnityEngine.Random.Range(0, 100);
                    if (prob < 70)
                    {
                        RegisterAction(new IdleAction(null), true);
                    }
                    else
                    {
                        Vector3 pos = new Vector2(
                            UnityEngine.Random.Range(-1, 1f),
                            UnityEngine.Random.Range(-1, 1f)
                        );
                        var targetPos = _agent.Pos + pos.normalized * UnityEngine.Random.Range(1, 5f);
                        RegisterAction(new HangingAction(targetPos), true);
                    }
                }
            }
        }

        private void OnActionCompleted(IAction action)
        {
            action.OnCompleted -= OnActionCompleted;
            action.OnActionProgress -= OnActionProgress;
            _curAction = null;
        }
    }
}