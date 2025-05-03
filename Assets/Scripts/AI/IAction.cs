using System;
using System.Collections.Generic;
using GameItem;
using Map;

namespace AI
{
    public enum InteractionPolicy
    {
        /// <summary>当前 action 不能被打断，也不能并行任何交互</summary>
        Exclusive,
        /// <summary>可被打断，但不与任何其它交互并行</summary>
        Interruptible,
        /// <summary>此行为可与其它指定行为并行（比如吃饭+对话）</summary>
        Concurrent
    }

    public interface IAction : IActionPool
    {
        List<IAction> PrecedingActions { get; }
        string ActionName { get; }
        bool Done { get; set; }
        bool CanBeInterrupted { get; }
        IAction NextAction { get; set; }
        bool Enable { get; }
        event Action<IAction, bool> OnCompleted;
        event Action<float> OnActionProgress;

        void Execute(Agent agent);
        void OnRegister(Agent agent);
        float Evaluate(Agent agent, HouseType houseType);
    }
}