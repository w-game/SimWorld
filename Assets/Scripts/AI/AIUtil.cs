using System.Collections.Generic;
using AI;
using GameItem;
using Map;

public class Behavior
{
    public static List<IAction> ScanEnvironment(Agent agent)
    {
        var items = agent.ScanAllItemsAround();
        var actions = new List<IAction>();
        foreach (var item in items)
        {
            actions.AddRange(item.ItemActions(agent));
        }

        return actions;
    }

    public static (IAction, float) Evaluate(Agent agent, List<IAction> actions, HouseType houseType)
    {
        var score = 0f;
        IAction action = null;
        foreach (var act in actions)
        {
            var actionScore = act.Evaluate(agent, houseType);
            if (actionScore > score)
            {
                score = actionScore;
                action = act;
            }
        }

        return (action, score);
    }
}