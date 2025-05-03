
using Citizens;
using GameItem;
using Map;
using Skill;
using UI.Elements;
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
            farmItem.Owner = agent.Owner;
            farmItem.ShowUI();
            agent.GetSkill<PlantSkill>().AddExp(10);
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
            CheckMoveToArroundPos(agent, _farmItem, () => { Target = _farmItem.Pos; });
        }

        protected override void DoExecute(Agent agent)
        {
            _farmItem.BePlant(_seedId);
            agent.Bag.RemoveItem(ConfigReader.GetConfig<PropConfig>(_seedId), 1);
            agent.GetSkill<PlantSkill>().AddExp(10);
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
                propItem.Owner = agent.Owner;
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
            ActionSpeed = 1f;
        }

        public override void OnRegister(Agent agent)
        {
            if (agent.Bag.CheckItem("PROP_TOOL_HANDBUCKET").Count == 0)
            {
                var bucket = ConfigReader.GetConfig<PropConfig>("PROP_TOOL_HANDBUCKET");
                MessageBox.I.ShowMessage("No Bucket!", bucket.icon, MessageType.Error);
                ActionFailed();
            }
            else
            {
                CheckMoveToArroundPos(agent, _wellItem, () => { Target = _wellItem.Pos; });
            }
        }

        protected override void DoExecute(Agent agent)
        {
            if (agent.Bag.CheckItem("PROP_TOOL_HANDBUCKET").Count > 0)
            {
                agent.Bag.RemoveItem(ConfigReader.GetConfig<PropConfig>("PROP_TOOL_HANDBUCKET"), 1);
                agent.Bag.AddItem(ConfigReader.GetConfig<PropConfig>("PROP_MATERIAL_HANDBUCKET_WATER"), 1);
            }
        }
    }

    // 驱虫、除草、
    // 施肥、浇水、收获
    public class WaterPlantAction : SingleActionBase
    {
        private FarmItem _farmItem;

        public override void OnGet(params object[] args)
        {
            _farmItem = args[0] as FarmItem;
            ActionName = "Water the plant";
            ActionSpeed = 1f;
        }

        public override void OnRegister(Agent agent)
        {
            if (agent.Bag.CheckItem("PROP_MATERIAL_HANDBUCKET_WATER").Count == 0)
            {
                var water = ConfigReader.GetConfig<PropConfig>("PROP_MATERIAL_HANDBUCKET_WATER");
                MessageBox.I.ShowMessage("No Water!", water.icon, MessageType.Error);
                ActionFailed();
            }
            else
            {
                CheckMoveToArroundPos(agent, _farmItem, () => { Target = _farmItem.Pos; });
            }
        }

        protected override void DoExecute(Agent agent)
        {
            agent.Bag.RemoveItem(ConfigReader.GetConfig<PropConfig>("PROP_MATERIAL_HANDBUCKET_WATER"), 1);
            agent.Bag.AddItem(new PropItem(ConfigReader.GetConfig<PropConfig>("PROP_TOOL_HANDBUCKET"), 1));
            _farmItem.BeWatered();
            agent.GetSkill<PlantSkill>().AddExp(10);
        }
    }

    public class HarvestAction : SingleActionBase
    {
        private PlantItem _plantItem;

        public override void OnGet(params object[] args)
        {
            _plantItem = args[0] as PlantItem;
            if (args.Length > 1)
            {
                var steal = (bool)args[1];
                if (steal)
                {
                    ActionName = "Harvest (Steal)";
                }
                else
                {
                    ActionName = "Harvest";
                }
            }
            else
            {
                ActionName = "Harvest";
            }

            ActionSpeed = 2f;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _plantItem.Pos, () => { Target = _plantItem.Pos; });
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
            agent.GetSkill<PlantSkill>().AddExp(20);
        }
    }

    public class WeedingAction : SingleActionBase
    {
        private PlantItem _plantItem;
        private PropConfig _hoeConfig;

        public override void OnGet(params object[] args)
        {
            _plantItem = args[0] as PlantItem;
            if (args.Length > 1)
            {
                _hoeConfig = args[1] as PropConfig;
            }
            if (_hoeConfig == null)
            {
                ActionName = "Weeding (No Hoe)";
                ActionSpeed = 1f;
            }
            else
            {
                ActionName = "Weeding (" + _hoeConfig.name + ")";
                ActionSpeed = 5f;
            }
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _plantItem, () => { Target = _plantItem.Pos; });
        }

        protected override void DoExecute(Agent agent)
        {
            _plantItem.Weeding();
            agent.GetSkill<PlantSkill>().AddExp(10);
        }
    }
}