
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

        public HoeAction(Vector3 targetPos, IHouse house) : base(20f)
        {
            _targetPos = targetPos;
            _house = house;
            ActionName = "Hoe the ground";
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _targetPos);
        }

        protected override void DoExecute(Agent agent)
        {
            var farmItem = GameItemManager.CreateGameItem<FarmItem>(
                GameManager.I.ConfigReader.GetConfig<BuildingConfig>("BUILDING_FARM"),
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

        public PlantAction(FarmItem farmItem, string seedId = "") : base(50f)
        {
            ActionName = "Plant seed";
            _farmItem = farmItem;
            _seedId = seedId;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _farmItem.Pos);
        }

        protected override void DoExecute(Agent agent)
        {
            _farmItem.Plant(_seedId);
            agent.Bag.RemoveItem(GameManager.I.ConfigReader.GetConfig<PropConfig>(_seedId), 1);
        }
    }

    public class RemovePlantAction : SingleActionBase
    {
        private PlantItem _plantItem;

        public RemovePlantAction(PlantItem plantItem, string actionName = "Remove the plant") : base(50f)
        {
            if (plantItem is TreeItem)
            {
                ActionName = actionName;
            }
            else
            {
                ActionName = actionName;
            }

            _plantItem = plantItem;
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
                var confg = GameManager.I.ConfigReader.GetConfig<PropConfig>(dropItem.id);
                var propItem = GameItemManager.CreateGameItem<PropGameItem>(confg, _plantItem.Pos, GameItemType.Static, count);
                propItem.ShowUI();
            }

            GameItemManager.DestroyGameItem(_plantItem);
        }
    }

    public class DrawWaterAction : SingleActionBase
    {
        private WellItem _wellItem;
        public DrawWaterAction(WellItem wellItem) : base(20f)
        {
            ActionName = "Draw water";
            _wellItem = wellItem;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(agent, _wellItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            var propItem = GameItemManager.CreateGameItem<PropGameItem>(GameManager.I.ConfigReader.GetConfig<PropConfig>("Water"), agent.Pos, GameItemType.Static, 1);
            propItem.ShowUI();
            agent.Brain.RegisterAction(new TakeItemInHand(propItem), true);
        }
    }

    // 驱虫、除草、
    // 施肥、浇水、收获
    public class WaterPlantAction : SingleActionBase
    {
        private Vector3 _targetPos;
        private PropGameItem _waterItem;

        public WaterPlantAction(Vector3 _targetPos) : base(50f)
        {
            ActionName = "Water the plant";
            this._targetPos = _targetPos;
        }

        public override void OnRegister(Agent agent)
        {
            // 判断手上是否有水
            var itemInHand = agent.GetItemInHand();
            if (itemInHand == null)
            {
                PrecedingActions.Add(new DrawWaterAction(agent.GetGameItem<WellItem>()));
            }
            PrecedingActions.Add(new CheckMoveToTarget(agent, _targetPos));
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

        public HarvestAction(PlantItem plantItem) : base(50f)
        {
            ActionName = "Harvest the plant";
            _plantItem = plantItem;
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
                var confg = GameManager.I.ConfigReader.GetConfig<PropConfig>(dropItem.id);
                var propItem = GameItemManager.CreateGameItem<PropGameItem>(confg, _plantItem.Pos, GameItemType.Static, count);
                propItem.ShowUI();
            }

            GameItemManager.DestroyGameItem(_plantItem);
        }
    }

    public class WeedingAction : SingleActionBase
    {
        private PlantItem _plantItem;

        public WeedingAction(PlantItem plantItem) : base(25f)
        {
            ActionName = "Weeding";
            _plantItem = plantItem;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(agent, _plantItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            _plantItem.Weeding();
        }
    }
}