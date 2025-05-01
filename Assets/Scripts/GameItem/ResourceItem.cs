using System.Collections.Generic;
using AI;
using Citizens;
using UnityEngine;

namespace GameItem
{

    public class ResourceItem : GameItemBase<ResourceConfig>
    {
        public ResourceItem(ResourceConfig config, Vector3 pos, bool random) : base(config, pos)
        {
            Walkable = true;
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>();
        }
    }

    public class SmallRockItem : ResourceItem
    {
        public SmallRockItem(ResourceConfig config, Vector3 pos, bool random) : base(config, pos, random)
        {

        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>
            {
                ActionPool.Get<CollectResourceAction>(this),
            };
        }
    }
}