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
        public Queue<IAction> ActiveActions { get; } = new Queue<IAction>();

        public IAction CurAction { get; private set; }

        public event Action<IAction> OnActionRegister;
        public event Action<IAction> OnActionUnregister;

        private Dictionary<State, float> _actionCooldowns = new();
        private float _actionCooldownTime = 10f;

        public event Action<float> OnActionProgress;

        private WorkAction _workAction;

        public void SetAgent(Agent agent)
        {
            _agent = agent;
        }

        public void RegisterAction(IAction action, bool force)
        {
            if (force)
            {
                ChangeCurAction(action);
            }
            else
            {
                ActiveActions.Enqueue(action);
            }
        }

        private void UnregisterAction(IAction action)
        {
            action.OnCompleted -= UnregisterAction;
            action.OnActionProgress -= OnActionProgress;
            action.OnActionFailed -= UnregisterAction;

            OnActionUnregister?.Invoke(action);
            CurAction = null;

            if (_workAction != null)
            {
                CurAction = _workAction;
                _workAction = null;
            }
        }

        private void ChangeCurAction(IAction action)
        {
            if (action == null)
                return;

            if (CurAction != null)
            {
                if (CurAction is WorkAction workAction)
                {
                    _workAction = workAction;
                }
                else
                {
                    UnregisterAction(CurAction);
                }
            }

            CurAction = action;
            CurAction.OnCompleted += UnregisterAction;
            CurAction.OnActionProgress += OnActionProgress;
            CurAction.OnActionFailed += UnregisterAction;

            action.OnRegister(_agent);
            OnActionRegister?.Invoke(action);
        }

        private bool CheckNormalState()
        {
            if (CurAction != null && !CurAction.CanBeInterrupted)
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

            if (CurAction is NormalAction curNormalAction)
            {
                float currentUtility = curNormalAction.State.CheckState(_agent.State.Mood.Value);
                Debug.Log($"当前行为效用: {currentUtility}, 新行为效用: {bestUtility}");
                if (bestUtility < currentUtility * 1.2f) // 只有新行为效用明显高，才换
                    return false;
            }

            else if (CurAction is WorkAction curWorkAction)
            {
                if (bestState.Name == "Social"
                || bestState.Name == "Mood"
                || bestState.Name == "Sleep"
                || bestState.Name == "Hygiene")
                    return false; // 当前行为是工作，且新状态不是社交或心情，则不换
            }

            Debug.Log($"当前行为: {CurAction?.ActionName}, 新行为: {bestState.Name}, 效用: {bestUtility}");

            switch (bestState.Name)
            {
                case "Health":
                    break;
                case "Hunger":
                    ResolveHungerBehavior();
                    break;
                case "Toilet":
                    var toiletAction = ActionPool.Get<ToiletAction>(_agent.State.Toilet);
                    RegisterAction(toiletAction, true);
                    break;
                case "Social":
                    // RegisterAction(new SocialAction(_agent.State.Social), true);
                    break;
                case "Mood":
                    // RegisterAction(new PlayAction(_agent.State.Mood), true);
                    break;
                case "Sleep":
                    RegisterAction(ActionPool.Get<SleepAction>(_agent.State.Sleep), true);
                    break;
                case "Hygiene":
                    // RegisterAction(new BathAction(null, _agent.State.Hygiene), true);
                    break;
            }

            return true;
        }

        private void ResolveHungerBehavior()
        {
            var foodItem = _agent.GetGameItem<FoodItem>();
            if (foodItem != null && foodItem.Owner == _agent.Citizen.Family)
            {
                RegisterAction(ActionPool.Get<EatAction>(foodItem, _agent.State.Hunger), true);
                return;
            }

            var type = MapManager.I.CheckMapAera(_agent.Pos);

            var candidates = new List<IAction>(){
                new CookAction(),
                new OrderFromRestaurant()
            };

            var (action, score) = Behavior.Evaluate(_agent, candidates, type);

            RegisterAction(action, true);
        }

        // 每个时间步更新状态，并根据评估周期进行行为检测
        public void Update()
        {
            var result = CheckNormalState();

            if (CurAction != null)
            {
                CurAction.Execute(_agent);
            }
            else if (ActiveActions.Count != 0)
            {
                ChangeCurAction(ActiveActions.Dequeue());
            }
            else if (!result)
            {
                var type = MapManager.I.CheckMapAera(_agent.Pos);
                var actions = Behavior.ScanEnvironment(_agent);
                var (action, score) = Behavior.Evaluate(_agent, actions, type);
                if (action != null)
                {
                    RegisterAction(action, true);
                }
                else
                {
                    var prob = UnityEngine.Random.Range(0, 100);
                    if (prob < 50)
                    {
                        // RegisterAction(new IdleAction(null), true);
                    }
                    else if (prob < 90)
                    {
                        // 散步、逛街

                    }
                    // else
                    // {
                    //     Vector3 pos = new Vector2(
                    //         UnityEngine.Random.Range(-1, 1f),
                    //         UnityEngine.Random.Range(-1, 1f)
                    //     );
                    //     var targetPos = _agent.Pos + pos.normalized * UnityEngine.Random.Range(1, 5f);
                    //     RegisterAction(new HangingAction(targetPos), true);
                    // }
                }
            }
        }
    }
}