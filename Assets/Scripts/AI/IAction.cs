using System;
using System.Collections.Generic;
using Citizens;
using Map;

namespace AI
{
    public interface IAction : IActionPool
    {
        List<IAction> PrecedingActions { get; }
        string ActionName { get; }
        bool Done { get; set; }
        bool CanBeInterrupted { get; }
        IAction NextAction { get; set; }
        bool Enable { get; }
        event Action<IAction> OnCompleted;
        event Action<float> OnActionProgress;
        event Action<IAction> OnActionFailed;

        void Execute(Agent agent);
        void OnRegister(Agent agent);
        float Evaluate(Agent agent, HouseType houseType);
    }
}