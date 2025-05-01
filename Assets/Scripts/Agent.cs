using System;
using System.Collections.Generic;
using AI;
using GameItem;
using UI.Elements;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Citizens
{
    public class State
    {
        public string Name { get; private set; }
        public float Value { get; private set; }
        public float Speed { get; private set; }

        public State(string name, float value, float speed)
        {
            Name = name;
            Value = value;
            Speed = speed;
        }

        public void Update()
        {
            Value -= Speed * GameTime.DeltaTime;
            if (Value < 0)
            {
                Value = 0;
            }
        }

        public virtual float CheckState(float mood)
        {
            float urgency = Mathf.Clamp01((100 - Value) / 100f);
            float utility = 100 - Value;
            float moodModifier = Mathf.Lerp(0.5f, 1.5f, mood / 100f);
            float finalScore = utility * (1 + urgency * 0.5f) * moodModifier;
            return finalScore;
        }

        internal void Increase(float increment)
        {
            Value += increment;
            if (Value > 100)
            {
                Value = 100;
            }
        }
    }

    public class SleepState : State
    {
        public SleepState(string name, float value, float speed) : base(name, value, speed)
        {
        }

        public override float CheckState(float mood)
        {
            float time = GameManager.I.GameTime.TimeInHours;
            float nightBoost = (time >= 20f || time < 6f) ? 1.25f : 1f; // 晚上8点到早上6点之间加成
            float urgency = Mathf.Clamp01((100 - Value) / 100f);
            float utility = 100 - Value;
            float moodModifier = Mathf.Lerp(0.5f, 1.5f, mood / 100f);
            float finalScore = utility * (1 + urgency * 0.5f) * moodModifier * nightBoost;
            return finalScore;
        }
    }

    public class Personality
    {
        public List<string> Hobbies { get; private set; } = new List<string>();
    }

    public class AgentState
    {
        public State Health { get; private set; }
        public State Hunger { get; private set; }
        public State Toilet { get; private set; }
        public State Social { get; private set; }
        public State Mood { get; private set; }
        public State Sleep { get; private set; }
        public State Hygiene { get; private set; }
        public Agent Agent { get; private set; }

        public event Action<AgentState> OnAgentStateChangedEvent;

        public AgentState(Agent agent)
        {
            Agent = agent;

            Health = new State("Health", 100, 0);
            Hunger = new State("Hunger", 100, 0.00463f);
            Toilet = new State("Toilet", 100, 0.00526f);
            Social = new State("Social", 100, 0.00347f);
            Mood = new State("Mood", 100, 0.00231f);
            Sleep = new State("Sleep", 100, 0.00174f);
            Hygiene = new State("Hygiene", 100, 0.00116f);
        }

        // 模拟状态随时间的消耗（例如每秒消耗一定值）
        public void UpdateState()
        {
            Hunger.Update();
            Toilet.Update();
            Sleep.Update();
            Hygiene.Update();
            Social.Update();
            Mood.Update();

            OnAgentStateChangedEvent?.Invoke(this);
        }
    }

    public class Money
    {
        public int Amount { get; private set; }

        public Money(int amount)
        {
            Amount = amount;
        }

        public void Add(int amount)
        {
            Amount += amount;
        }

        public void Subtract(int amount)
        {
            Amount -= amount;
        }
    }

    public class Agent : GameItemBase<ConfigBase>
    {
        public float MoveSpeed { get; private set; } = 5f;
        public int SightRange { get; private set; } = 8;

        public FamilyMember Citizen { get; private set; }
        public AgentState State { get; private set; }
        public Personality Personality { get; private set; }
        public AIController Brain { get; private set; } // 大脑
        public List<Vector2Int> _paths;
        public Dictionary<int, Dictionary<string, Schedule>> Schedules = new Dictionary<int, Dictionary<string, Schedule>>()
        {
            { 1, new Dictionary<string, Schedule>() },
            { 2, new Dictionary<string, Schedule>() },
            { 3, new Dictionary<string, Schedule>() },
            { 4, new Dictionary<string, Schedule>() },
            { 5, new Dictionary<string, Schedule>() },
            { 6, new Dictionary<string, Schedule>() },
            { 7, new Dictionary<string, Schedule>() }
        };

        private Schedule _currentSchedule;
        public PlayerController PlayerController { get; private set; }
        public Inventory Bag { get; private set; }
        public Money Money { get; private set; }

        public Agent(ConfigBase config, Vector3 pos, AIController brain, FamilyMember citizen) : base(null, pos)
        {
            Brain = brain;
            Brain.SetAgent(this);
            Citizen = citizen;
            Citizen.SetAgent(this);
            Owner = Citizen.Family;
            State = new AgentState(this);
            Bag = new Inventory(16);
            Money = new Money(100);
        }

        public override void ShowUI()
        {
            if (UI == null)
            {
                UI = GameManager.I.GameItemManager.ItemUIPool.Get<PlayerController>("Prefabs/Player", Pos);
                UI.Init(this);
                PlayerController = UI as PlayerController;
            }
        }

        public override void HideUI()
        {
            if (UI != null)
            {
                GameManager.I.GameItemManager.ItemUIPool.Release(UI, "Prefabs/Player");
                UI = null;
            }
        }

        public override void Update()
        {
            State.UpdateState();
            Move();
            Brain.Update();
            CheckSchedules();
        }

        private void CheckSchedules()
        {
            if (_currentSchedule != null)
            {
                _currentSchedule.Update(() =>
                {
                    _currentSchedule = null;
                });

                return;
            }

            var schedules = Schedules[GameManager.I.GameTime.Day];
            foreach (var schedule in schedules)
            {
                if (schedule.Value.Check(GameManager.I.GameTime.CurrentTime, GameManager.I.GameTime.Day))
                {
                    _currentSchedule = schedule.Value;
                    break;
                }
            }
        }

        private void Move()
        {
            if (GameManager.I.CurrentAgent != this) return;
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            if (moveX == 0 && moveY == 0)
            {
                return;
            }
            Vector2 target = PlayerController.Rb.position + new Vector2(moveX, moveY) * MoveSpeed * Time.fixedDeltaTime;

            if (!MapManager.I.IsWalkable(target))
            {
                target = PlayerController.Rb.position + new Vector2(moveX, 0) * MoveSpeed * Time.fixedDeltaTime;
                if (!MapManager.I.IsWalkable(target))
                {
                    target = PlayerController.Rb.position + new Vector2(0, moveY) * MoveSpeed * Time.fixedDeltaTime;
                    if (!MapManager.I.IsWalkable(target))
                    {
                        return;
                    }
                }
            }

            _pos = target;

            PlayerController.MoveTo(target);
        }

        public void MoveToTarget(Vector2 pos)
        {
            if (!MapManager.I.IsWalkable(pos)) return;

            var cellPos = MapManager.I.WorldPosToCellPos(Pos);
            var targetCellPos = MapManager.I.WorldPosToCellPos(pos);
            _paths = AStar.FindPath(cellPos, targetCellPos, (pos) =>
            {
                return MapManager.I.IsWalkable(new Vector3(pos.x, pos.y));
            });
        }

        public void MoveToTarget()
        {
            if (_paths == null || _paths.Count == 0) return;
            var targetPosition = new Vector3(_paths[0].x + 0.5f, _paths[0].y + 0.5f);
            Pos = Vector3.MoveTowards(Pos, targetPosition, MoveSpeed * Time.deltaTime);

            if (Vector3.Distance(Pos, targetPosition) < 0.1f)
            {
                _paths.RemoveAt(0);
            }
        }

        public bool CheckArriveTargetPos()
        {
            return _paths == null || _paths.Count == 0;
        }

        private FoodItem GetFoodItem()
        {
            foreach (var item in Bag.Items)
            {
                if (item.Config.id.Contains("PROP_FOOD"))
                {

                }
            }

            return null;
        }

        private T BFSItem<T>() where T : IGameItem
        {
            var visitedPositions = new HashSet<Vector2>();
            var queue = new Queue<Vector2>();
            queue.Enqueue(Pos);
            visitedPositions.Add(Pos);

            while (queue.Count > 0)
            {
                Vector2 currentPos = queue.Dequeue();
                // 检查当前位置是否有目标物品
                var items = GameManager.I.GameItemManager.GetItemsAtPos(currentPos);
                foreach (var item in items)
                {
                    if (item is T tItem) return tItem;
                }

                // 向周围位置扩散（限定最大距离避免无穷扩散）
                foreach (var dir in new Vector2[] {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right })
                {
                    Vector2 nextPos = currentPos + dir;
                    if (!visitedPositions.Contains(nextPos)
                        && Vector2.Distance(nextPos, Pos) <= SightRange)
                    {
                        visitedPositions.Add(nextPos);
                        queue.Enqueue(nextPos);
                    }
                }
            }
            return default;
        }

        public T GetGameItem<T>() where T : class, IGameItem
        {
            var item = BFSItem<T>();
            if (typeof(T) == typeof(FoodItem))
            {
                var foodItem = GetFoodItem();
                return foodItem as T;
            }

            return item;
        }

        public TableItem FindNearestTableItem()
        {
            return null;
            // throw new NotImplementedException();
        }


        // 根据兴趣爱好寻找最近可交互的娱乐物品
        public IGameItem FindByHobby()
        {
            throw new NotImplementedException();
        }

        public void TakeItemInHand(IGameItem item)
        {
            // item.transform.SetParent(handItem);
            // item.transform.localPosition = Vector3.zero;
        }

        internal void RegisterSchedule(Schedule newSchedule, string scheduleName)
        {
            foreach (var day in newSchedule.Days)
            {
                var schedules = Schedules[day];
                if (schedules.Count == 0)
                {
                    schedules.Add(scheduleName, newSchedule);
                    return;
                }
                else
                {
                    // 检测时间是否重叠
                    foreach (var sch in new Dictionary<string, Schedule>(schedules))
                    {
                        if ((newSchedule.StartTime > sch.Value.StartTime && newSchedule.StartTime < sch.Value.EndTime) ||
                            (newSchedule.EndTime > sch.Value.StartTime && newSchedule.EndTime < sch.Value.EndTime) ||
                            (newSchedule.StartTime < sch.Value.StartTime && newSchedule.EndTime > sch.Value.EndTime))
                        {
                            if (newSchedule.Priority > sch.Value.Priority)
                            {
                                schedules.Remove(sch.Key);
                                schedules.Add(scheduleName, newSchedule);
                            }
                            else
                            {
                                Debug.Log("时间冲突，无法添加新日程");
                                return;
                            }
                        }
                    }
                }
            }
        }

        internal PropGameItem GetItemInHand()
        {
            return null;
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            throw new NotImplementedException();
        }

        internal PropConfig GetOrder(Agent consumer)
        {
            throw new NotImplementedException();
        }

        internal List<IGameItem> ScanAllItemsAround()
        {
            List<IGameItem> foundItems = new List<IGameItem>();

            for (int i = 0; i < SightRange; i++)
            {
                for (int j = 0; j < SightRange; j++)
                {
                    var pos = new Vector2(Pos.x + i, Pos.y + j);
                    var items = GameManager.I.GameItemManager.GetItemsAtPos(pos);
                    foundItems.AddRange(items);
                }
            }
            return foundItems;
        }

        internal void MoveToArroundPos(IGameItem item, Action callback)
        {
            var arroundPosList = item.ArroundPosList();
            if (arroundPosList.Count == 0)
            {
                MessageBox.I.ShowMessage("No space to put the item", "Textures/Path", MessageType.Error);
                return;
            }
            var pos = arroundPosList[0];
            var action = ActionPool.Get<CheckMoveToTarget>(this, pos);
            SystemAction system = new SystemAction("Move to", a =>
            {
                callback?.Invoke();
            }, action);
            Brain.RegisterAction(system, true);
        }

        internal void UnregisterSchedule(string scheduleName)
        {
            foreach (var day in Schedules.Keys)
            {
                var schedules = Schedules[day];
                if (schedules.ContainsKey(scheduleName))
                {
                    schedules.Remove(scheduleName);
                }
            }
        }
    }
}