
using Citizens;
using GameItem;
using Map;
using UnityEngine;

namespace AI
{
    public class HoeAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 20;
        public override int ProgressTimes { get; protected set; } = 1;

        private Vector3 _targetPos;
        private House _house;

        public HoeAction(Vector3 targetPos, House house)
        {
            _targetPos = targetPos;
            _house = house;
            ActionName = "Hoe the ground";
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_targetPos));
        }

        protected override void DoExecute(Agent agent)
        {
            var farmItem = GameItemManager.CreateGameItem<FarmItem>(
                GameManager.I.ConfigReader.GetConfig<BuildingConfig>("BUILDING_FARM"),
                _targetPos + new Vector3(0.5f, 0.5f, 0),
                GameItemType.Static);
            farmItem.ShowUI();
        }
    }

    public class PlantAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 50;
        public override int ProgressTimes { get; protected set; } = 1;

        private Vector3 _targetPos;
        private string _seedId;

        public PlantAction(Vector3 targetPos, string seedId)
        {
            _targetPos = targetPos;
            ActionName = "Plant the seed";
            _seedId = seedId;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_targetPos));
        }

        protected override void DoExecute(Agent agent)
        {
            PlantItem plantItem = GameItemManager.CreateGameItem<PlantItem>(
                GameManager.I.ConfigReader.GetConfig<ResourceConfig>(_seedId),
                _targetPos + new Vector3(0.5f, 0.5f, 0),
                GameItemType.Static);
            plantItem.ShowUI();
        }
    }

    public class RemovePlantAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 50;
        public override int ProgressTimes { get; protected set; } = 1;

        private PlantItem _plantItem;

        public RemovePlantAction(PlantItem plantItem)
        {
            if (plantItem is TreeItem)
            {
                ActionName = "Chop the tree";
            }
            else
            {
                ActionName = "Remove the plant";
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
            foreach (var dropItem in _plantItem.Config.dropItems)
            {
                var confg = GameManager.I.ConfigReader.GetConfig<PropConfig>(dropItem.id);
                var propItem = GameItemManager.CreateGameItem<PropGameItem>(confg, _plantItem.Pos, GameItemType.Static, dropItem.count);
                propItem.ShowUI();
            }

            GameItemManager.DestroyGameItem(_plantItem);
        }
    }

    public class DrawWaterAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 20;
        public override int ProgressTimes { get; protected set; } = 1;

        private WellItem _wellItem;
        public DrawWaterAction(WellItem wellItem)
        {
            ActionName = "Draw water";
            _wellItem = wellItem;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_wellItem.Pos));
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
    public class WaterPlantAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 50;
        public override int ProgressTimes { get; protected set; } = 1;

        private Vector3 _targetPos;
        private PropGameItem _waterItem;

        public WaterPlantAction(Vector3 _targetPos)
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
            PrecedingActions.Add(new CheckMoveToTarget(_targetPos));
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

    public class HarvestAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 50;
        public override int ProgressTimes { get; protected set; } = 1;

        private PlantItem _plantItem;

        public HarvestAction(PlantItem plantItem)
        {
            ActionName = "Harvest the plant";
            _plantItem = plantItem;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_plantItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            agent.HarvestItem(_plantItem);
        }
    }

    public class WeedingAction : ActionBase
    {
        public override float ProgressSpeed { get; protected set; } = 10;
        public override int ProgressTimes { get; protected set; } = 1;

        private PlantItem _plantItem;

        public WeedingAction(PlantItem plantItem)
        {
            ActionName = "Weeding";
            _plantItem = plantItem;
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_plantItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            _plantItem.Weeding();
        }
    }
}