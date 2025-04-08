using System.Collections.Generic;
using AI;
using GameItem;

public class GameItemActions
{
    public static List<ActionBase> GetActionByItem(GameItemBase item)
    {
        var actions = new List<ActionBase>();
        if (item is PlantItem plantItem)
        {
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
        else if (item is PropGameItem propItem)
        {
            if (item is FoodItem foodItem)
            {
                actions.Add(new EatAction(foodItem));
            }

            actions.Add(new PutIntoBag(propItem));
        }

        return actions;
    }
}