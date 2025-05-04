using Citizens;
using GameItem;
using Map;
using UnityEngine;

namespace AI
{
    public class IdleAction : SingleActionBase
    {
        private IGameItem _item;

        public override void OnGet(params object[] args)
        {
            if (args.Length > 0)
            {
                _item = args[0] as IGameItem;
            }
            ActionName = "Idle";
            ActionSpeed = 50f;
        }

        public override void OnRegister(Agent agent)
        {
            if (_item != null)
            {
                PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _item.Pos));
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

                            if (MapManager.I.TryGetBuildingItem(targetPos, out var item) && MapManager.I.IsWalkable(targetPos))
                            {
                                if (item.House == buildingItem.House)
                                {
                                    PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, targetPos));
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

        public override void OnGet(params object[] args)
        {
            _targetPos = (Vector3)args[0];
            ActionName = "Hanging";
            ActionSpeed = 50f;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _targetPos));
        }

        protected override void DoExecute(Agent agent)
        {
            Done = true;
        }
    }
}