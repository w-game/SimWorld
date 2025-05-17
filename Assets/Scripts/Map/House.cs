using System;
using System.Collections.Generic;
using Citizens;
using GameItem;
using UnityEngine;

namespace Map
{
    public enum HouseType
    {
        None,
        House,
        Farm,
        Teahouse,
        Restaurant,
        Factory,
        Shop,
        Office,
        School,
        Hospital,
        Park
    }

    public interface IHouse
    {
        List<Vector2Int> Blocks { get; }
        Vector2Int Size { get; }
        HouseType HouseType { get; }
        City City { get; }
        Dictionary<Vector2Int, FurnitureItem> FurnitureItems { get; }
        Vector2Int MinPos { get; }
        List<Vector2Int> CommercialPos { get; }
        public Vector2Int DoorPos { get; }

        bool TryGetFurniture<T>(out T furnitureItem) where T : FurnitureItem;
        bool TryGetFurnitures<T>(out List<T> furnitureItems) where T : FurnitureItem;
    }

    public class CartonHouse : IHouse
    {
        public List<Vector2Int> Blocks { get; private set; } // 房屋的所有块
        public Vector2Int Size { get; private set; } // 房屋大小，影响房屋半径
        public City City { get; private set; } // 房屋所在的城市
        public HouseType HouseType { get; private set; } // 房屋类型

        public Vector2 RandomPos => new Vector2(_chunkRand.Next(MinPos.x + 1, MinPos.x + Size.x - 2), _chunkRand.Next(MinPos.y + 1, MinPos.y + Size.y - 2)); // 随机位置

        private System.Random _chunkRand;
        public Vector2Int MinPos { get; private set; } // 房屋最小坐标
        public Vector2Int MidPos => new Vector2Int(MinPos.x + Size.x / 2, MinPos.y + Size.y / 2); // 房屋中心坐标

        public RoomConfig RoomConfig { get; private set; } // 房间配置

        public Dictionary<Vector2Int, FurnitureItem> FurnitureItems { get; private set; } = new Dictionary<Vector2Int, FurnitureItem>(); // 家具物品
        private List<BuildingItem> _buildingItems = new List<BuildingItem>(); // 房屋内的建筑物
        public List<Vector2Int> CommercialPos { get; private set; } = new List<Vector2Int>(); // 商业位置
        public Family Owner { get; private set; }

        public Vector2Int DoorPos { get; private set; } // 门的位置

        public CartonHouse(List<Vector2Int> blocks, RoomConfig config, Vector2Int minPos, City city, System.Random chunkRand)
        {
            Blocks = blocks; // 转换为全局坐标
            RoomConfig = config;
            Size = new Vector2Int(config.width, config.height); // 房屋大小
            MinPos = minPos; // 房屋最小坐标
            City = city;
            _chunkRand = chunkRand;

            Enum.TryParse<HouseType>(config.type, true, out var houseType);
            HouseType = houseType;
            CalcRooms();
        }

        public void SetOwner(Family owner)
        {
            Owner = owner;
            foreach (var item in FurnitureItems.Values)
            {
                item.Owner = owner;
            }

            foreach (var item in _buildingItems)
            {
                item.Owner = owner;
            }
        }

        private void CalcRooms()
        {
            DoorPos = MinPos + new Vector2Int(Size.x / 2, Size.y / 2);
            for (int i = 0; i < RoomConfig.width; i++)
            {
                for (int j = 0; j < RoomConfig.height; j++)
                {
                    var pos = new Vector2Int(i, j) + MinPos;
                    BuildingItem buildingItem = null;
                    if (RoomConfig.layout[i + j * RoomConfig.width] == 0)
                    {
                        buildingItem = GameItemManager.CreateGameItem<WallItem>(
                            ConfigReader.GetConfig<BuildingConfig>("BUILDING_WALL"),
                            new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                            GameItemType.Static,
                            this);
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 1)
                    {
                        buildingItem = GameItemManager.CreateGameItem<FloorItem>(
                            ConfigReader.GetConfig<BuildingConfig>("BUILDING_FLOOR"),
                            new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                            GameItemType.Static,
                            this
                        );
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 2)
                    {
                        buildingItem = GameItemManager.CreateGameItem<DoorItem>(
                            ConfigReader.GetConfig<BuildingConfig>("BUILDING_DOOR"),
                            new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                            GameItemType.Static,
                            this
                        );
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 3)
                    {
                        // new FloorItem(this, ConfigReader.GetConfig<BuildingConfig>("BUILDING_FLOOR"), new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0));
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 4)
                    {
                        buildingItem = GameItemManager.CreateGameItem<CommercialItem>(
                            ConfigReader.GetConfig<BuildingConfig>("BUILDING_FLOOR"),
                            new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                            GameItemType.Static,
                            this
                        );
                        CommercialPos.Add(pos);
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == -2)
                    {
                        buildingItem = GameItemManager.CreateGameItem<FarmItem>(
                            ConfigReader.GetConfig<BuildingConfig>("BUILDING_FARM"),
                            new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                            GameItemType.Static,
                            this
                        );
                    } 
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 5)
                    {
                        buildingItem = GameItemManager.CreateGameItem<FrontDoorItem>(
                            ConfigReader.GetConfig<BuildingConfig>("BUILDING_DOOR"),
                            new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                            GameItemType.Static,
                            this
                        );
                        DoorPos = pos;
                    }

                    if (buildingItem != null)
                    {
                        _buildingItems.Add(buildingItem);
                    }
                }
            }

            foreach (var furniture in RoomConfig.furnitures)
            {
                var pos = new Vector2Int(furniture.pos.x, furniture.pos.y) + MinPos;

                var config = ConfigReader.GetConfig<BuildingConfig>(furniture.id);
                var type = Type.GetType($"GameItem.{config.type}Item");
                var furnitureItem = GameItemManager.CreateGameItem<FurnitureItem>(
                    type,
                    config,
                    new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                    GameItemType.Static);

                FurnitureItems.Add(pos, furnitureItem);
            }
        }

        internal float DistanceTo(Vector3 pos)
        {
            return Vector2.Distance(new Vector2(pos.x, pos.y), new Vector2(MinPos.x, MinPos.y));
        }

        public bool TryGetFurniture<T>(out T furnitureItem) where T : FurnitureItem
        {
            foreach (var item in FurnitureItems)
            {
                if (item.Value is T)
                {
                    furnitureItem = item.Value as T;
                    return true;
                }
            }

            furnitureItem = null;
            return false;
        }

        public bool TryGetFurnitures<T>(out List<T> furnitureItems) where T : FurnitureItem
        {
            furnitureItems = new List<T>();
            foreach (var item in FurnitureItems)
            {
                if (item.Value is T)
                {
                    furnitureItems.Add(item.Value as T);
                }
            }

            return furnitureItems.Count > 0;
        }
    }
}