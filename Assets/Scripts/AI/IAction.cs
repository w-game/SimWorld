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
        event Action<IAction> OnCompleted;

        float CalculateUtility(Agent agent);
        void Execute(Agent agent);
        void OnRegister(Agent agent);
    }
}