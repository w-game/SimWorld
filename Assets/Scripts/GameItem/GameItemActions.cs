using System.Collections.Generic;
using AI;
using GameItem;

public class GameItemActions
{
    public static List<ActionBase> GetActionByItem(GameItemBase item)
    {
        var actions = new List<ActionBase>();
        if (item.PropItem.Config.id.Contains("PLANT"))
        {
            var plantItem = item as PlantItem;
            switch (plantItem.GrowthStage)
            {
                case 0:
                case 1:
                    actions.Add(new RemovePlantAction(plantItem));
                    break;
                default:
                    break;
            }
        }
        else if (item.PropItem.Config.id.Contains("PROP"))
        {
            if (item is FoodItem foodItem)
            {
                actions.Add(new EatAction(foodItem));
            }
            // else if (item is ToolItem toolItem)
            // {
            //     actions.Add(new UseToolAction(toolItem));
            // }

            actions.Add(new PutIntoBag(item));
        }

        return actions;
    }
}