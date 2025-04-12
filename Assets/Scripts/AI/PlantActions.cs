
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
            MapManager.I.SetMapTile(_targetPos, BlockType.Farm, MapLayer.Building, MapManager.I.farmTiles);
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
            // PlantItem plantItem = new PlantItem(GameManager.I.ConfigReader.GetConfig<>(_seedId), _targetPos);

            // var cellPos = MapManager.I.WorldPosToCellPos(_targetPos);
            // var plantObj = GameManager.I.InstantiateObject("Prefabs/GameItems/PlantItem", new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0));
            // var plant = plantObj.GetComponent<PlantItem>();
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

    // 驱虫、除草、
    // 施肥、浇水、收获
}