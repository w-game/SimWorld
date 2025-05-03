using System;
using System.Collections.Generic;
using GameItem;
using UnityEngine;

namespace Citizens
{
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

        public Dictionary<Agent, int> Relationships = new Dictionary<Agent, int>();

        public event Action<AgentState> OnAgentStateChangedEvent;

        public AgentState(Agent agent)
        {
            Agent = agent;

            Health = new State("Health", 100, 0, Agent);
            Hunger = new HungerState("Hunger", 100, 0.00463f, Agent);
            Toilet = new ToiletState("Toilet", 100, 0.00526f, Agent);
            Social = new SocialState("Social", 100, 0.00347f, Agent);
            Mood = new State("Mood", 100, 0.00231f, Agent);
            Sleep = new SleepState("Sleep", 100, 0.00174f, Agent);
            Hygiene = new HygieneState("Hygiene", 100, 0.00116f, Agent);
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

    public class State
    {
        public string Name { get; private set; }
        public float Value { get; private set; }
        public float Speed { get; private set; }
        public Agent Agent { get; private set; }

        public State(string name, float value, float speed, Agent agent)
        {
            Name = name;
            Value = value;
            Speed = speed;
            Agent = agent;
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

    public class HungerState : State
    {
        public HungerState(string name, float value, float speed, Agent agent) : base(name, value, speed, agent)
        {
        }

        public override float CheckState(float mood)
        {
            var foodItem = Agent.GetGameItem<FoodItem>();
            if (foodItem == null)
            {
                var stoveItem = Agent.GetGameItem<StoveItem>();

                if (stoveItem == null)
                {
                    return 0f; // 没有食物或炉子，效用为0
                }
                else if (stoveItem.Owner == Agent.Owner || stoveItem.Owner == null)
                {
                    return base.CheckState(mood);
                }
                else
                {
                    return 0f; // 有炉子但不是自己的，效用为0
                }
            }

            return base.CheckState(mood);
        }
    }

    public class ToiletState : State
    {
        public ToiletState(string name, float value, float speed, Agent agent) : base(name, value, speed, agent)
        {
        }

        public override float CheckState(float mood)
        {
            var toiletItem = Agent.GetGameItem<ToiletItem>();
            if (toiletItem == null)
            {
                return 0f;
            }

            return base.CheckState(mood);
        }
    }

    public class HygieneState : State
    {
        public HygieneState(string name, float value, float speed, Agent agent) : base(name, value, speed, agent)
        {
        }

        public override float CheckState(float mood)
        {
            var hygieneItem = Agent.GetGameItem<HygieneItem>();
            if (hygieneItem == null)
            {
                return 0f;
            }

            return base.CheckState(mood);
        }
    }

    public class SleepState : State
    {
        public SleepState(string name, float value, float speed, Agent agent) : base(name, value, speed, agent)
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

            // 如果没有床，降低效用
            var bedItem = Agent.GetGameItem<BedItem>();
            if (bedItem == null)
            {
                finalScore *= 0.5f; // 没有床，效用降低50%
            }
            return finalScore;
        }
    }
    
    public class SocialState : State
    {
        public SocialState(string name, float value, float speed, Agent agent) : base(name, value, speed, agent)
        {
        }

        public override float CheckState(float mood)
        {
            if (Agent.OtherAround.Count == 1)
            {
                return 0f; // 没有其他人，效用为0
            }

            return base.CheckState(mood);
        }
    }
}