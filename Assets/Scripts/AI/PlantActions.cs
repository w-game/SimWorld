
using Citizens;
using GameItem;
using Map;
using UI.Models;
using UnityEngine;

namespace AI
{
    public class HoeAction : SingleActionBase
    {
        private Vector3 _targetPos;
        private IHouse _house;

        public override void OnGet(params object[] args)
        {
            _targetPos = (Vector3)args[0];
            _house = args[1] as IHouse;
            ActionName = "Hoe the ground";
            ActionSpeed = 1f;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _targetPos, () => { Target = _targetPos; });
        }

        protected override void DoExecute(Agent agent)
        {
            var farmItem = GameItemManager.CreateGameItem<FarmItem>(
                ConfigReader.GetConfig<BuildingConfig>("BUILDING_FARM"),
                _targetPos,
                GameItemType.Static,
                _house);
            farmItem.ShowUI();
        }
    }

    public class PlantAction : SingleActionBase
    {
        private FarmItem _farmItem;
        private string _seedId;

        public override void OnGet(params object[] args)
        {
            _farmItem = args[0] as FarmItem;
            _seedId = args[1] as string;
            ActionName = "Plant seed";
            ActionSpeed = 1f;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _farmItem.Pos, () => { Target = _farmItem.Pos; });
        }

        protected override void DoExecute(Agent agent)
        {
            _farmItem.BePlant(_seedId);
            agent.Bag.RemoveItem(ConfigReader.GetConfig<PropConfig>(_seedId), 1);
        }
    }

    public class RemovePlantAction : SingleActionBase
    {
        private PlantItem _plantItem;

        public override void OnGet(params object[] args)
        {
            _plantItem = args[0] as PlantItem;
            ActionName = "Remove the plant";
            ActionSpeed = 50f;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _plantItem.Pos);
        }

        protected override void DoExecute(Agent agent)
        {
            Log.LogInfo("PlantActions", "PrecedingActions count: " + PrecedingActions.Count);
            var dropItems = _plantItem.CheckDropItems();
            foreach (var (dropItem, count) in dropItems)
            {
                var confg = ConfigReader.GetConfig<PropConfig>(dropItem.id);
                var propItem = GameItemManager.CreateGameItem<PropGameItem>(confg, _plantItem.Pos, GameItemType.Static, count);
                propItem.ShowUI();
            }

            GameItemManager.DestroyGameItem(_plantItem);
        }
    }

    public class DrawWaterAction : SingleActionBase
    {
        private WellItem _wellItem;

        public override void OnGet(params object[] args)
        {
            _wellItem = args[0] as WellItem;
            ActionName = "Draw water";
            ActionSpeed = 20f;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _wellItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            var propItem = GameItemManager.CreateGameItem<PropGameItem>(ConfigReader.GetConfig<PropConfig>("Water"), agent.Pos, GameItemType.Static, 1);
            propItem.ShowUI();
            agent.Brain.RegisterAction(ActionPool.Get<TakeItemInHand>(propItem), true);
        }
    }

    // 驱虫、除草、
    // 施肥、浇水、收获
    public class WaterPlantAction : SingleActionBase
    {
        private Vector3 _targetPos;
        private PropGameItem _waterItem;

        public override void OnGet(params object[] args)
        {
            _targetPos = (Vector3)args[0];
            ActionName = "Water the plant";
            ActionSpeed = 50f;
        }

        public override void OnRegister(Agent agent)
        {
            // 判断手上是否有水
            var itemInHand = agent.GetItemInHand();
            if (itemInHand == null)
            {
                PrecedingActions.Add(ActionPool.Get<DrawWaterAction>(agent.GetGameItem<WellItem>()));
            }
            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _targetPos));
        }

        protected override void DoExecute(Agent agent)
        {
            MapManager.I.TryGetBuildingItem(_targetPos, out var buildingItem);
            if (buildingItem is FarmItem farmItem)
            {
                _waterItem = agent.GetItemInHand();

                if (_waterItem != null)
                {
                    farmItem.BeWatered(_waterItem);
                }
                else
                {
                    OnActionFailedEvent();
                }
            }
        }
    }

    public class HarvestAction : SingleActionBase
    {
        private PlantItem _plantItem;

        public override void OnGet(params object[] args)
        {
            _plantItem = args[0] as PlantItem;
            ActionName = "Harvest the plant";

            ActionSpeed = 50f;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _plantItem.Pos);
        }

        protected override void DoExecute(Agent agent)
        {
            Log.LogInfo("PlantActions", "PrecedingActions count: " + PrecedingActions.Count);
            var dropItems = _plantItem.CheckDropItems();
            foreach (var (dropItem, count) in dropItems)
            {
                var confg = ConfigReader.GetConfig<PropConfig>(dropItem.id);
                var propItem = GameItemManager.CreateGameItem<PropGameItem>(confg, _plantItem.Pos, GameItemType.Static, count);
                propItem.Owner = agent.Owner;
                propItem.ShowUI();
            }

            GameItemManager.DestroyGameItem(_plantItem);
        }
    }

    public class WeedingAction : SingleActionBase
    {
        private PlantItem _plantItem;

        public override void OnGet(params object[] args)
        {
            _plantItem = args[0] as PlantItem;
            ActionName = "Weeding";

            ActionSpeed = 25f;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(ActionPool.Get<CheckMoveToTarget>(agent, _plantItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            _plantItem.Weeding();
        }
    }
}