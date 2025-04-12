using Citizens;
using GameItem;
using UnityEngine;

namespace AI
{
    public class IdleAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 2f;
        public override int ProgressTimes { get; protected set; } = 1;

        private GameItemBase _item;

        public IdleAction(GameItemBase item)
        {
            ActionName = "Idle";
            _item = item;
            ProgressSpeed = UnityEngine.Random.Range(0.5f, 10f);
            ProgressTimes = 1;
        }

        public override void OnRegister(Agent agent)
        {
            if (_item != null)
            {
                PrecedingActions.Add(new CheckMoveToTarget(_item.Pos));
            }
        }

        protected override void DoExecute(Agent agent)
        {
            Done = true;
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