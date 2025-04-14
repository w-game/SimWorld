using System;
using System.Collections.Generic;
using System.Linq;
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
                    ResolveHungerBehavior();
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

        private void ResolveHungerBehavior()
        {
            var foodItem = _agent.GetGameItem<FoodItem>();
            if (foodItem != null && foodItem.Owner == _agent.Ciziten.Family)
            {
                RegisterAction(new EatAction(foodItem, _agent.State.Hunger), true);
                return;
            }

            var type = MapManager.I.CheckMapAera(_agent.Pos);

            switch (type)
            {
                case Map.HouseType.House:
                    var stoveItem = _agent.GetGameItem<StoveItem>();
                    if (stoveItem != null)
                    {
                        RegisterAction(new CookAction(stoveItem), true);
                        return;
                    }
                    break;
                case Map.HouseType.None:
                case Map.HouseType.Restaurant:
                    var city = MapManager.I.CartonMap.GetCity(_agent.Pos);
                    if (city != null)
                    {
                        var houses = city.GetHouses(Map.HouseType.Restaurant);
                        if (houses.Count > 0)
                        {
                            var restaurant = houses.OrderBy(a => a.DistanceTo(_agent.Pos)).First();
                            var restaurantProperty = Property.Properties[restaurant] as RestaurantProperty;
                            var availableCommercialPos = restaurantProperty.GetAvailableCommericalPos();
                            if (availableCommercialPos.Count > 0)
                            {
                                var pos = availableCommercialPos[UnityEngine.Random.Range(0, availableCommercialPos.Count)];
                                var moveToTarget = new CheckMoveToTarget(new Vector3(pos.x, pos.y), "Restaurant");
                                moveToTarget.NextAction = new WaitForAvailableSitAction(Property.Properties[restaurant] as RestaurantProperty);
                                RegisterAction(moveToTarget, true);
                            }
                            return;
                        }
                    }
                    // var store = GameManager.I.PropertyManager.FindNearestFoodShop(_agent.Pos);
                    // if (store != null)
                    // {
                    //     RegisterAction(new BuyFoodAction(store), true);
                    //     return;
                    // }
                    break;
            }
        }

        // 每个时间步更新状态，并根据评估周期进行行为检测
        public void Update()
        {
            var result = CheckNormalState();

            if (_curAction != null)
            {
                _curAction.Execute(_agent);
            }
            else if (_activeActionsFirst.Count != 0)
            {
                _curAction = _activeActionsFirst.Dequeue();
                Debug.Log($"当前行为: {_curAction.ActionName}");
                _curAction.OnCompleted += OnActionCompleted;
                _curAction.OnActionProgress += OnActionProgress;
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

                switch (type)
                {
                    case Map.HouseType.Teahouse:
                        var chairItem = _agent.GetGameItem<ChairItem>();
                        if (chairItem != null && chairItem.Using == null)
                        {
                            RegisterAction(new SitAction(chairItem), true);
                        }
                        break;
                    case Map.HouseType.House:

                        var bookItem = _agent.GetGameItem<BookItem>();
                        if (bookItem != null)
                        {
                            RegisterAction(new ReadAction(bookItem), true);
                        }

                        // 古代其他行为：读书、写字、绘画、弹琴、下棋、听曲、写作


                        // 手工艺：编草编、刺绣、剪纸、陶艺、木工、金工、石工


                        // 检测宣纸
                        var paperItem = _agent.GetGameItem<PaperItem>();
                        if (paperItem != null)
                        {
                            // RegisterAction(new WriteAction(paperItem), true);
                            // RegisterAction(new PaintAction(null), true);
                        }
                        // RegisterAction(new PlayMusicAction(null), true);
                        // RegisterAction(new ChessAction(null), true);
                        // RegisterAction(new ListenMusicAction(null), true);
                        break;
                    case Map.HouseType.Farm:
                        break;
                    default:
                        break;
                }

                // TODO: 发呆、按照兴趣指派行为等
                if (GameManager.I.CurrentAgent != _agent)
                {
                    var prob = UnityEngine.Random.Range(0, 100);
                    if (prob < 50)
                    {
                        RegisterAction(new IdleAction(null), true);
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

        private void OnActionCompleted(IAction action)
        {
            action.OnCompleted -= OnActionCompleted;
            action.OnActionProgress -= OnActionProgress;

            if (_curAction.NextAction != null)
            {
                _curAction = _curAction.NextAction;
                _curAction.OnCompleted += OnActionCompleted;
                _curAction.OnActionProgress += OnActionProgress;
            }
            else
            {
                _curAction = null;
            }
        }
    }
}