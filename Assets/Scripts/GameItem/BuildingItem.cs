using System.Collections.Generic;
using System.Drawing;
using AI;
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
        public House House { get; private set; }
        public List<TileBase> Tiles { get; protected set; }
        public BuildingItem(BuildingConfig config, Vector3 pos, House house) : base(config, pos)
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

        protected override List<IAction> ActionsOnClick()
        {
            return new List<IAction>()
            {
                new SystemAction("View Room Details", a =>
                {
                    var model = IModel.GetModel<PopHouseDetailsModel>(House);
                    model.ShowUI();
                })
            };
        }

        public override void Destroy()
        {
            MapManager.I.SetMapTile(Pos, MapLayer.Building, null);
        }
    }

    public class WallItem : BuildingItem
    {
        public override bool Walkable => false;
        public WallItem(BuildingConfig config, Vector3 pos, House house) : base(config, pos, house)
        {
            Tiles = MapManager.I.wallTiles;
        }
    }

    public class FloorItem : BuildingItem
    {
        public FloorItem(BuildingConfig config, Vector3 pos, House house) : base(config, pos, house)
        {
            Tiles = MapManager.I.floorTiles;
        }
    }

    public class DoorItem : BuildingItem
    {
        public DoorItem(BuildingConfig config, Vector3 pos, House house) : base(config, pos, house)
        {
            Tiles = new List<TileBase> { MapManager.I.doorTile };
        }
    }

    public class CommercialItem : BuildingItem
    {
        public CommercialItem(BuildingConfig config, Vector3 pos, House house) : base(config, pos, house)
        {
            Tiles = MapManager.I.floorTiles;
        }
    }

    public class FarmItem : BuildingItem
    {
        public FarmItem(BuildingConfig config, Vector3 pos, House house) : base(config, pos, house)
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
    }
}