using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AI;
using Citizens;
using Map;
using NUnit.Framework;
using UI.Models;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameItem
{
    public class BlueprintItem : GameItemBase<BuildingConfig>
    {
        public override bool Walkable => true;

        public BlueprintItem(BuildingConfig config, Vector3 pos) : base(config, pos)
        {
            Size = new Vector2Int(config.size[0], config.size[1]);
        }

        public override void ShowUI()
        {
            base.ShowUI();
            UI.SetRenderer(Config.icon, 0.4f);
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>
            {
                ActionPool.Get<CraftBuildingItemAction>(this),
            };
        }

        public void Place()
        {

        }
    }

    public class BuildingItem : GameItemBase<BuildingConfig>
    {
        public override bool Walkable => true;
        public IHouse House { get; private set; }
        public List<TileBase> Tiles { get; protected set; }
        private bool _isPlaced;
        public BuildingItem(BuildingConfig config, Vector3 pos, IHouse house) : base(config, pos)
        {
            House = house;

            Size = new Vector2Int(config.size[0], config.size[1]);
        }

        public override void ShowUI()
        {
            if (_isPlaced)
            {
                return;
            }
            MapManager.I.SetMapTile(Pos, MapLayer.Building, Tiles);
            _isPlaced = true;
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>
            {
                ActionPool.Get<RemoveBuildingItemAction>(this),
            };
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            var actions = base.ActionsOnClick(agent);
            actions.Add(new SystemAction("View Room Details", a =>
            {
                var model = IModel.GetModel<PopHouseDetailsModel>(House);
                model.ShowUI();
            }));
            return actions;
        }

        public override void HideUI()
        {
            MapManager.I.SetMapTile(Pos, MapLayer.Building, null);
            _isPlaced = false;
        }

        public override void Destroy()
        {
            HideUI();
        }
    }

    public class WallItem : BuildingItem
    {
        public override bool Walkable => false;
        public WallItem(BuildingConfig config, Vector3 pos, IHouse house) : base(config, pos, house)
        {
            Tiles = MapManager.I.wallTiles;
        }
    }

    public class FloorItem : BuildingItem
    {
        public FloorItem(BuildingConfig config, Vector3 pos, IHouse house) : base(config, pos, house)
        {
            Tiles = MapManager.I.floorTiles;
        }
    }

    public class DoorItem : BuildingItem
    {
        public DoorItem(BuildingConfig config, Vector3 pos, IHouse house) : base(config, pos, house)
        {
            Tiles = new List<TileBase> { MapManager.I.doorTile };
        }
    }

    public class CommercialItem : BuildingItem
    {
        public CommercialItem(BuildingConfig config, Vector3 pos, IHouse house) : base(config, pos, house)
        {
            Tiles = MapManager.I.floorTiles;
        }
    }

    public class FarmItem : BuildingItem, ISelectItem
    {
        public PlantItem PlantItem => GameManager.I.GameItemManager.TryGetItemAtPos<PlantItem>(Pos);
        public FarmItem(BuildingConfig config, Vector3 pos, IHouse house) : base(config, pos, house)
        {
            Tiles = MapManager.I.farmTiles;
        }

        internal void BeWatered()
        {
            MapManager.I.SetMapTile(Pos, MapLayer.Building, MapManager.I.farmWateredTiles);
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            var action = ActionPool.Get<PlantAction>(this, "");
            var waterAction = ActionPool.Get<WaterPlantAction>(this);
            return base.ItemActions(agent).Concat(new List<IAction> { action, waterAction }).ToList();
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            var waterAction = ActionPool.Get<WaterPlantAction>(this);

            if (PlantItem != null)
            {
                return new List<IAction>() { waterAction };
            }

            return new List<IAction>()
            {
                waterAction,
                new SystemAction("Plant Seed", a =>
                {
                    var model = IModel.GetModel<PopSelectSeedModel>(this, PropType.Seed);
                    model.ShowUI();
                })
            };
        }

        public void OnSelected(string seedId)
        {
            if (string.IsNullOrEmpty(seedId))
            {
                return;
            }
            GameManager.I.CurrentAgent.Brain.RegisterAction(ActionPool.Get<PlantAction>(this, seedId), true);
        }

        public void BePlant(string seedId)
        {
            var cropSeedConfig = ConfigReader.GetConfig<CropSeedConfig>(seedId);
            PlantItem plantItem = GameItemManager.CreateGameItem<PlantItem>(
                ConfigReader.GetConfig<ResourceConfig>(cropSeedConfig.targets["plant"]),
                Pos + new Vector3(0.5f, 0.5f, 0),
                GameItemType.Static,
                false);

            plantItem.Owner = Owner;

            plantItem.ShowUI();
        }
    }
}