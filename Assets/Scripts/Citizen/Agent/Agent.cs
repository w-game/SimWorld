using System;
using System.Collections.Generic;
using System.Linq;
using AI;
using Citizens;
using Skill;
using UI.Elements;
using UnityEngine;
using UnityEngine.Events;

namespace GameItem
{
    public class Personality
    {
        public List<string> Hobbies { get; private set; } = new List<string>();
    }

    public class Money
    {
        public Agent Agent { get; private set; }
        public int Amount { get; private set; }

        public Money(int amount, Agent agent)
        {
            Agent = agent;
            Amount = amount;
        }

        public void Add(int amount)
        {
            Amount += amount;
            if (Agent == GameManager.I.CurrentAgent)
                MessageBox.I.ShowMessage($"Got {amount} money", "Textures/Money", MessageType.Info);
        }

        public void Subtract(int amount)
        {
            Amount -= amount;
            if (Agent == GameManager.I.CurrentAgent)
                MessageBox.I.ShowMessage($"Cost {amount} money", "Textures/Money", MessageType.Info);
        }
    }

    public class Agent : DynamicItem
    {
        public float MoveSpeed { get; private set; } = 3f;
        public int SightRange { get; private set; } = 9;
        private List<Vector2Int> _sights = new List<Vector2Int>();
        public int MaxScanCount { get; private set; } = 10;

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

        public Dictionary<Type, SkillBase> Skills { get; private set; } = new Dictionary<Type, SkillBase>();

        public IGameItem ItemInHand { get; private set; }

        public Agent(ConfigBase config, Vector3 pos, AIController brain, FamilyMember citizen) : base(null, pos)
        {
            Brain = brain;
            Brain.SetAgent(this);
            Citizen = citizen;
            Citizen.SetAgent(this);
            Owner = Citizen.Family;
            State = new AgentState(this);
            Bag = new Inventory(16);
            Money = new Money(1000, this);
            Personality = new Personality();

            for (int i = -SightRange / 2; i <= SightRange / 2; i++)
            {
                for (int j = -SightRange / 2; j <= SightRange / 2; j++)
                {
                    _sights.Add(new Vector2Int((int)pos.x, (int)pos.y));
                }
            }

            Bag.AddItem(ConfigReader.GetConfig<PropConfig>("PROP_TOOL_HANDBUCKET"), 1);
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

        private Vector3? _nextPos;
        private Vector2Int? _targetCellPos;
        public void CalcMovePaths(Vector3 targetWorldPos, int maxSteps = 5000)
        {
            if (!MapManager.I.IsWalkable(Pos) || !MapManager.I.IsWalkable(targetWorldPos)) return;

            var startCell = MapManager.I.WorldPosToCellPos(Pos);
            var targetCell = MapManager.I.WorldPosToCellPos(targetWorldPos);

            int attempt = 0;
            const int maxAttempts = 5;

            while (attempt < maxAttempts)
            {
                _paths = AStar.FindPath(startCell, targetCell, p => MapManager.I.IsWalkable(new Vector3(p.x, p.y)), maxSteps);

                if (_paths != null && _paths.Count > 1)
                {
                    var cell = _paths[1];
                    _nextPos = new Vector3(cell.x + 0.5f, cell.y + 0.5f);
                    _targetCellPos = targetCell;
                    return;
                }

                maxSteps *= 2;
                attempt++;
            }

            _paths = null;
            _nextPos = null;
            _targetCellPos = null;
        }

        public void MoveToTarget(Vector3 pos)
        {
            if (_paths == null)
            {
                if (Vector3.SqrMagnitude(Pos - pos) > 0.0025f)
                {
                    Pos = Vector3.MoveTowards(Pos, pos, MoveSpeed * Time.deltaTime);
                }
                return;
            }
            if (_nextPos == null || _targetCellPos == null) return;

            if (!MapManager.I.IsWalkable(_nextPos.Value) || _targetCellPos.Value != MapManager.I.WorldPosToCellPos(pos))
            {
                CalcMovePaths(pos);
                if (_paths == null) return;
            }

            Pos = Vector3.MoveTowards(Pos, _nextPos.Value, MoveSpeed * Time.deltaTime);

            if (Vector3.SqrMagnitude(Pos - _nextPos.Value) < 0.01f)
            {
                _paths.RemoveAt(0);
                if (_paths.Count > 1)
                {
                    var cell = _paths[1];
                    _nextPos = new Vector3(cell.x + 0.5f, cell.y + 0.5f);
                }
                else
                {
                    _nextPos = null;
                    _targetCellPos = null;
                }
            }
        }

        private List<IGameItem> BFSItem()
        {
            List<IGameItem> foundItems = new List<IGameItem>();
            var visitedPositions = new HashSet<Vector2>();
            var queue = new Queue<Vector2>();
            queue.Enqueue(Pos);
            visitedPositions.Add(Pos);

            while (queue.Count > 0)
            {
                Vector2 currentPos = queue.Dequeue();
                // 检查当前位置是否有目标物品
                var items = GameManager.I.GameItemManager.GetItemsAtPos(currentPos);

                foundItems.AddRange(items);

                // 向周围位置扩散（限定最大距离避免无穷扩散）
                foreach (var dir in new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
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
            return foundItems;
        }

        private Dictionary<Type, List<IGameItem>> _gameItems = new Dictionary<Type, List<IGameItem>>();
        private Vector3 _lastScanPos;

        public T GetGameItem<T>() where T : class, IGameItem
        {
            if ((Pos - _lastScanPos).sqrMagnitude > SightRange * SightRange)
            {
                var foundItems = BFSItem();
                _gameItems.Clear();

                foreach (var foundItem in foundItems)
                {
                    if (_gameItems.ContainsKey(foundItem.GetType()))
                    {
                        _gameItems[foundItem.GetType()].Add(foundItem);
                    }
                    else
                    {
                        _gameItems.Add(foundItem.GetType(), new List<IGameItem> { foundItem });
                    }
                }
                _lastScanPos = Pos;
            }

            if (_gameItems.TryGetValue(typeof(T), out var items) && items.Count > 0)
            {
                // 如果物品在视野范围内，直接返回
                foreach (var item in items)
                {
                    if (item is T tItem)
                        return tItem;
                }
            }

            return null;
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
            ItemInHand = item;
        }

        public void PutItemToTarget(IGameItem item)
        {
            var handItem = ItemInHand;
            ItemInHand = null;

            handItem.Pos = item.Pos + new Vector3(0.5f, 0.4f);
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
            return new List<IAction>();
        }

        internal PropConfig GetOrder(Agent consumer)
        {
            throw new NotImplementedException();
        }

        public List<IGameItem> ScanAllItemsAround(bool scanAll = false)
        {
            List<IGameItem> foundItems = new List<IGameItem>();

            var visitedPositions = new HashSet<Vector2>();
            var queue = new Queue<Vector2>();
            queue.Enqueue(Pos);
            visitedPositions.Add(Pos);

            while (queue.Count > 0)
            {
                Vector2 currentPos = queue.Dequeue();
                // 检查当前位置是否有目标物品
                var items = GameManager.I.GameItemManager.GetItemsAtPos(currentPos);
                foundItems.AddRange(items);

                // 向周围位置扩散（限定最大距离避免无穷扩散）
                foreach (var dir in new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
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

            if (!scanAll)
            {
                foundItems.Shuffle();
                foundItems = foundItems.Take(MaxScanCount).ToList();
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

        public bool CheckInteraction(Agent agent, Type actionType)
        {
            State.Relationships.TryGetValue(agent, out int value);
            // 根据双方关系度、性格、当前行为等因素计算交互成功率
            // 例如：如果双方关系度较高，则成功率增加
            // 如果双方关系不深、性格外向相对内向成功率高
            // 如果当前行为不忙绿，则成功率增加

            // 1. 并发性检查：对话只能与并行行为并行
            // var talkAction = ActionPool.Get<TalkAction>(this, other);
            // if (!other.CanParticipate(talkAction))
            // {
            //     return false;
            // }

            // 2. 关系度因子：-100~100 映射到 0~1
            State.Relationships.TryGetValue(agent, out int relation);
            float relationFactor = Mathf.Clamp01((relation + 100f) / 200f);

            // 3. 心情因子：0~100 映射到 0.5~1.5
            float myMood = State.Mood != null ? State.Mood.Value : 100f;
            float otherMood = agent.State.Mood != null ? agent.State.Mood.Value : 100f;
            float moodFactor = Mathf.Lerp(0.5f, 1.5f, (myMood + otherMood) / 2f / 100f);

            // 4. 兴趣相同加成
            float hobbyBonus = 0f;
            if (Personality.Hobbies.Intersect(agent.Personality.Hobbies).Any())
            {
                hobbyBonus = 0.1f;
            }

            // 5. 计算总成功率
            float baseRate = 0.5f;
            float successRate = Mathf.Clamp01(baseRate * (0.5f + relationFactor) * moodFactor + hobbyBonus);

            // 6. 随机判定
            return UnityEngine.Random.value < successRate;
        }

        private DialogElement _dialogElement;
        public void ShowConvarsation(string conversation, UnityAction callback)
        {
            // 显示对话框
            var chatUI = GameManager.I.GameItemManager.ItemUIPool.Get<DialogElement>("Prefabs/UI/Elements/DialogElement", Pos + Vector3.up, UI.transform);
            chatUI.ShowTextByCharacter(conversation, callback);
            _dialogElement = chatUI;
        }

        public void HideDialog()
        {
            _dialogElement?.Hide();
            _dialogElement = null;
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            if (Citizen.Job is Owner owner && owner.Property is ShopProperty)
            {
                return new List<IAction>()
                {
                    ActionPool.Get<CheckInteractionAction>(this, typeof(ChatAction), "Chat"),
                    ActionPool.Get<CheckInteractionAction>(this, typeof(TradeAction), "Trade")
                };
            }

            return new List<IAction>()
            {
                ActionPool.Get<CheckInteractionAction>(this, typeof(ChatAction), "Chat")
            };
        }

        public bool CheckSkillLevel<T>(int targetLevel) where T : SkillBase
        {
            if (Skills.TryGetValue(typeof(T), out var skill))
            {
                return skill.Level >= targetLevel;
            }

            return false;
        }
        
        public T GetSkill<T>() where T : SkillBase
        {
            if (Skills.TryGetValue(typeof(T), out var skill))
            {
                return skill as T;
            }
            else
            {
                var newSkill = Activator.CreateInstance<T>();
                Skills[typeof(T)] = newSkill;
                return newSkill;
            }
        }

        public void Eat(FoodItem foodItem)
        {
            if (foodItem == null)
            {
                Debug.LogError("Food item is null");
                return;
            }

            foodItem.FoodTimes--;
            State.Hunger.Increase(foodItem.FoodValue / foodItem.FoodTimes);

            if (foodItem.FoodTimes <= 0)
            {
                foodItem.AddCount(-1);
            }
        }
    }
}