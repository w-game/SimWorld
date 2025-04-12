using System;
using System.Collections.Generic;
using Citizens;

namespace AI
{
    public interface IAction
    {
        List<IAction> PrecedingActions { get; }
        string ActionName { get; }
        float ProgressSpeed { get; }
        bool Done { get; set; }
        bool CanBeInterrupted { get; }
        event Action<IAction> OnCompleted;
        event Action<float> OnActionProgress;
        void Execute(Agent agent);
        void OnRegister(Agent agent);
    }
}