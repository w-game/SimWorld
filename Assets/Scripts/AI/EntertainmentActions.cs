using Citizens;
using GameItem;
using Map;
using UnityEngine;

namespace AI
{
    public class IdleAction : SingleActionBase
    {
        private IGameItem _item;

        public IdleAction(IGameItem item)
        {
            ActionName = "Idle";
            _item = item;
        }

        public override void OnRegister(Agent agent)
        {
            if (_item != null)
            {
                PrecedingActions.Add(new CheckMoveToTarget(agent, _item.Pos));
            }
            else
            {
                if (MapManager.I.TryGetBuildingItem(agent.Pos, out var buildingItem))
                {
                    if (buildingItem.House.HouseType == HouseType.House)
                    {
                        do
                        {
                            var offset = new Vector3(Random.Range(-3f, 3f),
                                Random.Range(-3f, 3f));
                            var targetPos = agent.Pos + offset;

                            if (MapManager.I.TryGetBuildingItem(targetPos, out var item))
                            {
                                if (item.House == buildingItem.House)
                                {
                                    PrecedingActions.Add(new CheckMoveToTarget(agent, targetPos));
                                    break;
                                }
                            }
                        }
                        while (true);
                    }
                }
            }
        }

        protected override void DoExecute(Agent agent)
        {
        }
    }

    public class HangingAction : SingleActionBase
    {

        private Vector3 _targetPos;

        public HangingAction(Vector3 targetPos)
        {
            ActionName = "Hanging";
            _targetPos = targetPos;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(agent, _targetPos));
        }

        protected override void DoExecute(Agent agent)
        {
            Done = true;
        }
    }
}