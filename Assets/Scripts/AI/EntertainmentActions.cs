using Citizens;
using GameItem;
using Map;
using UnityEngine;

namespace AI
{
    public class IdleAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 2f;
        public override int ProgressTimes { get; protected set; } = -1;

        private IGameItem _item;

        public IdleAction(IGameItem item)
        {
            ActionName = "Idle";
            _item = item;
            ProgressTimes = -1;
        }

        public override void OnRegister(Agent agent)
        {
            if (_item != null)
            {
                PrecedingActions.Add(new CheckMoveToTarget(_item.Pos));
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
                                    PrecedingActions.Add(new CheckMoveToTarget(targetPos));
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
            ProgressTimes = 1;
            ProgressSpeed = Random.Range(0.5f, 3f);
        }
    }

    public class HangingAction : ActionBase
    {

        public override float ProgressSpeed { get; protected set; } = 100f;
        public override int ProgressTimes { get; protected set; } = -1;
        private Vector3 _targetPos;

        public HangingAction(Vector3 targetPos)
        {
            ActionName = "Hanging";
            _targetPos = targetPos;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_targetPos));
        }

        protected override void DoExecute(Agent agent)
        {
            Done = true;
        }
    }
}