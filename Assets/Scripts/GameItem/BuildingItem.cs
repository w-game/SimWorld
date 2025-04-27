using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AI;
using Citizens;
using Map;
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
                new CraftBuildingItemAction(this)
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
        public BuildingItem(BuildingConfig config, Vector3 pos, IHouse house) : base(config, pos)
        {
            House = house;

            Size = new Vector2Int(config.size[0], config.size[1]);
        }

        public override void ShowUI()
        {
            MapManager.I.SetMapTile(Pos, MapLayer.Building, Tiles);
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            return new List<IAction>
            {
                new RemoveBuildingItemAction(this),
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

        public override void Destroy()
        {
            MapManager.I.SetMapTile(Pos, MapLayer.Building, null);
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

    public class FarmItem : BuildingItem
    {
        public PlantItem PlantItem => GameManager.I.GameItemManager.TryGetItemAtPos<PlantItem>(Pos);
        public FarmItem(BuildingConfig config, Vector3 pos, IHouse house) : base(config, pos, house)
        {
            Tiles = MapManager.I.farmTiles;
        }

        internal void BeWatered(PropGameItem waterItem)
        {
            if (waterItem != null)
            {
                MapManager.I.SetMapTile(Pos, MapLayer.Building, MapManager.I.farmWateredTiles);
            }
        }

        public override List<IAction> ItemActions(IGameItem agent)
        {
            var action = new PlantAction(this);
            return base.ItemActions(agent).Concat(new List<IAction> { action }).ToList();
        }

        public override List<IAction> ActionsOnClick(Agent agent)
        {
            if (PlantItem != null)
            {
                return new List<IAction>();
            }

            return new List<IAction>()
            {
                new SystemAction("Plant Seed", a =>
                {
                    var model = new PopSelectSeedModel(selectedSeedId =>
                    {
                        if (string.IsNullOrEmpty(selectedSeedId))
                        {
                            return;
                        }

                        GameManager.I.CurrentAgent.Brain.RegisterAction(new PlantAction(this, selectedSeedId), true);
                    });
                    model.ShowUI();
                })
            };
        }

        public void Plant(string seedId)
        {
            var cropSeedConfig = GameManager.I.ConfigReader.GetConfig<CropSeedConfig>(seedId);
            PlantItem plantItem = GameItemManager.CreateGameItem<PlantItem>(
                GameManager.I.ConfigReader.GetConfig<ResourceConfig>(cropSeedConfig.target),
                Pos + new Vector3(0.5f, 0.5f, 0),
                GameItemType.Static,
                false);

            plantItem.ShowUI();
        }
    }
}