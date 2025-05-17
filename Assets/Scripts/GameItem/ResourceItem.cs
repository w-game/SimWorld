using System.Collections.Generic;
using AI;

namespace GameItem
{

    public class ResourceItem : GameItemBase<ResourceConfig>
    {
        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>();
        }
    }

    public class SmallRockItem : ResourceItem
    {
        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>
            {
                ActionPool.Get<CollectResourceAction>(this),
            };
        }
    }
}