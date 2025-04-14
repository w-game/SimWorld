using System;
using System.Collections.Generic;
using AI;
using Map;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameItem
{
    public class BuildingItem : StaticGameItem
    {
        public House House { get; private set; }
        public List<TileBase> Tiles { get; protected set; }
        public BuildingItem(House house, ConfigBase config, Vector3 pos) : base(config, pos)
        {
            House = house;
        }

        public override void ShowUI()
        {
            MapManager.I.SetMapTile(Pos, MapLayer.Building, Tiles);
        }

        public override List<IAction> ItemActions()
        {
            return new List<IAction>
            {
                new ViewHouseDetailsAction(House)
            };
        }
    }

    public class WallItem : BuildingItem
    {
        public WallItem(House house, ConfigBase config, Vector3 pos) : base(house, config, pos)
        {
            Tiles = MapManager.I.wallTiles;
        }
    }

    public class FloorItem : BuildingItem
    {
        public FloorItem(House house, ConfigBase config, Vector3 pos) : base(house, config, pos)
        {
            Tiles = MapManager.I.floorTiles;
        }
    }

    public class DoorItem : BuildingItem
    {
        public DoorItem(House house, ConfigBase config, Vector3 pos) : base(house, config, pos)
        {
            Tiles = new List<TileBase> { MapManager.I.doorTile };
        }
    }

    public class CommercialItem : BuildingItem
    {
        public CommercialItem(House house, ConfigBase config, Vector3 pos) : base(house, config, pos)
        {
            Tiles = MapManager.I.floorTiles;
        }
    }
    
    public class FarmItem : BuildingItem
    {
        public FarmItem(House house, ConfigBase config, Vector3 pos) : base(house, config, pos)
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