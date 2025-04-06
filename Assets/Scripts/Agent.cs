using System;
using AI;
using GameItem;
using UnityEngine;

namespace Citizens
{
    public class AgentState
    {
        public float Health = 100;
        public float Hunger = 100;
        public float Toilet = 100;
        public float Social = 100;
        public float Mood = 100;
        public float Sleep = 100;
        public float Hygiene = 100;
        public Agent Agent { get; private set; }

        public Vector3 Pos => Agent.transform.position;
        
        public static event Action<AgentState> OnAgentStateChangedEvent;

        public AgentState(Agent agent)
        {
            Agent = agent;
        }

        // 模拟状态随时间的消耗（例如每秒消耗一定值）
        public void UpdateState(float deltaTime)
        {
            Hunger -= deltaTime * 2;    // 饥饿度随时间降低
            Toilet -= deltaTime * 2;    // 厕所度随时间降低
            Sleep  -= deltaTime * 1.5f;  // 睡眠随时间降低
            Hygiene -= deltaTime * 1;    // 清洁度随时间降低

            // 保证数值不低于0
            Hunger = Math.Max(0, Hunger);
            Toilet = Math.Max(0, Toilet);
            Sleep  = Math.Max(0, Sleep);
            Hygiene = Math.Max(0, Hygiene);

            OnAgentStateChangedEvent?.Invoke(this);
        }
    }
    
    public class Agent : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private Vector2 targetPosition;
        private bool isMoving = false;
        public float MoveSpeed { get; private set; } = 10f;

        private AIController _aiController;
        public AgentState State { get; private set; }
        [SerializeField] private Transform handItem;

        private void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            targetPosition = _rb.position;
            State = new AgentState(this);
            _aiController = new AIController(this);
        }

        private void FixedUpdate()
        {
            // 如果正在移动，则逐步向目标点移动
            if (isMoving)
            {
                Vector2 newPosition = Vector2.MoveTowards(_rb.position, targetPosition, MoveSpeed * Time.deltaTime);
                _rb.MovePosition(newPosition);

                // 当距离足够接近目标点时，停止移动
                if (Vector2.Distance(newPosition, targetPosition) < 0.001f)
                {
                    isMoving = false;
                }
            }
        }

        private void Update()
        {
            State.UpdateState(Time.deltaTime);
            _aiController.Update();
        }

        public void RegisterAction(ActionBase action)
        {
            // 添加动作到行为树
            _aiController.RegisterAction(action, true);
        }

        // 设置目标点并开始移动
        public void MoveToTarget(Vector2 pos)
        {
            targetPosition = pos;
            isMoving = true;
        }

        public FoodItem GetFoodItem()
        {
            float searchRadius = 20f;
            Vector2 currentPos = transform.position;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(currentPos, searchRadius);
            FoodItem nearestFood = null;
            float minDistance = Mathf.Infinity;

            foreach (Collider2D collider in colliders)
            {
                var foodItem = collider.GetComponent<FoodItem>();
                if (foodItem != null)
                {
                    float distance = Vector2.Distance(currentPos, collider.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestFood = foodItem;
                    }
                }
            }
            return nearestFood;
        }

        public TableItem FindNearestTableItem()
        {
            return null;
            // throw new NotImplementedException();
        }
        
        public GameItemBase FindNearestWC()
        {
            throw new NotImplementedException();
        }
        
        // 根据兴趣爱好寻找最近可交互的娱乐物品
        public GameItemBase FindByHobby()
        {
            throw new NotImplementedException();
        }

        public void TakeItemInHand(GameItemBase item)
        {
            item.transform.SetParent(handItem);
            item.transform.localPosition = Vector3.zero;
        }
    }
}