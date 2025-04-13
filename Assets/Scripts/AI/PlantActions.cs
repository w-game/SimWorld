
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

        public HoeAction(Vector3 targetPos)
        {
            _targetPos = targetPos;
            ActionName = "Hoe the ground";
        }

        public override void OnRegister(Agent agent)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_targetPos));
        }

        protected override void DoExecute(Agent agent)
        {
            MapManager.I.SetMapTile(_targetPos, MapLayer.Building, MapManager.I.farmTiles, BuildingType.Farm);
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
            PlantItem plantItem = new PlantItem(GameManager.I.ConfigReader.GetConfig<ResourceConfig>(_seedId), _targetPos + new Vector3(0.5f, 0.5f, 0));
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
            PrecedingActions.Add(new CheckMoveToTarget(_plantItem.Pos));
        }

        protected override void DoExecute(Agent agent)
        {
            Log.LogInfo("PlantActions", "PrecedingActions count: " + PrecedingActions.Count);
            if (_plantItem is TreeItem treeItem)
            {
                foreach (var dropItem in treeItem.ConvtertConfig<ResourceConfig>().dropItems)
                {
                    var confg = GameManager.I.ConfigReader.GetConfig<PropConfig>(dropItem.id);
                    var propItem = new PropGameItem(confg, dropItem.count);
                    propItem.ShowUI();
                }
            }
            else
            {

            }

            _plantItem.Destroy();
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
            var propItem = new PropGameItem(GameManager.I.ConfigReader.GetConfig<PropConfig>("Water"), 1);
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

        public WaterPlantAction(Vector3 targetPos)
        {
            ActionName = "Water the plant";
            _targetPos = targetPos;
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
            var buildingType = MapManager.I.CheckBuildingType(_targetPos);
            if (buildingType == BuildingType.Farm)
            {
                _waterItem = agent.GetItemInHand();

                if (_waterItem != null)
                {
                    MapManager.I.SetMapTile(_targetPos, MapLayer.Building, MapManager.I.farmWateredTiles, BuildingType.Farm);
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
}