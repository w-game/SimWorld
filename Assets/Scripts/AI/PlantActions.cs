
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

        public override float CalculateUtility(AgentState state)
        {
            return 0.5f;
        }

        public override void OnRegister(AgentState state)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_targetPos));
        }

        protected override void DoExecute(AgentState state)
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

        public override float CalculateUtility(AgentState state)
        {
            return 0.5f;
        }

        public override void OnRegister(AgentState state)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_targetPos));
        }

        protected override void DoExecute(AgentState state)
        {
            var cellPos = MapManager.I.WorldPosToCellPos(_targetPos);
            var plantObj = GameManager.I.InstantiateObject("Prefabs/GameItems/PlantItem", new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0));
            var plant = plantObj.GetComponent<PlantItem>();
            MapManager.I.RegisterGameItem(plant);
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

        public override float CalculateUtility(AgentState state)
        {
            return 0.5f;
        }

        public override void OnRegister(AgentState state)
        {
            PrecedingActions.Add(new CheckMoveToTarget(_plantItem.transform.position));
        }

        protected override void DoExecute(AgentState state)
        {
            Log.LogInfo("PlantActions", "PrecedingActions count: " + PrecedingActions.Count);
            if (_plantItem is TreeItem treeItem)
            {
                foreach (var dropItem in treeItem.ConvtertConfig<ResourceConfig>().dropItems)
                {
                    var propGo = GameManager.I.InstantiateObject("Prefabs/GameItems/PropItem", _plantItem.transform.position);
                    var propItem = propGo.AddComponent<PropGameItem>();
                    var confg = GameManager.I.ConfigReader.GetConfig<PropConfig>(dropItem.id);
                    propItem.Init(confg, dropItem.count);
                    MapManager.I.RegisterGameItem(propItem);
                }
            }
            else
            {

            }

            MapManager.I.RemoveGameItem(_plantItem);
        }
    }

    // 驱虫、除草、
    // 施肥、浇水、收获
}