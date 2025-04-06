using System.Collections.Generic;
using AI;
using GameItem;

public class GameItemActions
{
    public static List<ActionBase> GetActionByItem(GameItemBase item)
    {
        var actions = new List<ActionBase>();
        if (item.ItemId.Contains("PLANT"))
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
        else if (item.ItemId.Contains("PROP"))
        {

        }

        return actions;
    }
}