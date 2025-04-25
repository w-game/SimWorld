using System;
using System.Collections.Generic;
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

    public class House
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
        public List<Vector2Int> CommercialPos { get; private set; } = new List<Vector2Int>(); // 商业位置

        public House(List<Vector2Int> blocks, RoomConfig config, Vector2Int minPos, City city, System.Random chunkRand)
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

        private void CalcRooms()
        {
            for (int i = 0; i < RoomConfig.width; i++)
            {
                for (int j = 0; j < RoomConfig.height; j++)
                {
                    var pos = new Vector2Int(i, j) + MinPos;
                    if (RoomConfig.layout[i + j * RoomConfig.width] == 0)
                    {
                        GameItemManager.CreateGameItem<WallItem>(
                            GameManager.I.ConfigReader.GetConfig<BuildingConfig>("BUILDING_WALL"),
                            new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                            GameItemType.Static,
                            this);
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 1)
                    {
                        GameItemManager.CreateGameItem<FloorItem>(
                            GameManager.I.ConfigReader.GetConfig<BuildingConfig>("BUILDING_FLOOR"),
                            new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                            GameItemType.Static,
                            this
                        );
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 2)
                    {
                        GameItemManager.CreateGameItem<DoorItem>(
                            GameManager.I.ConfigReader.GetConfig<BuildingConfig>("BUILDING_DOOR"),
                            new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                            GameItemType.Static,
                            this
                        );
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 3)
                    {
                        // new FloorItem(this, GameManager.I.ConfigReader.GetConfig<BuildingConfig>("BUILDING_FLOOR"), new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0));
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 4)
                    {
                        GameItemManager.CreateGameItem<CommercialItem>(
                            GameManager.I.ConfigReader.GetConfig<BuildingConfig>("BUILDING_FLOOR"),
                            new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0),
                            GameItemType.Static,
                            this
                        );
                        CommercialPos.Add(pos);
                    }
                }
            }

            foreach (var furniture in RoomConfig.furnitures)
            {
                var pos = new Vector2Int(furniture.pos[0], furniture.pos[1]) + MinPos;

                var config = GameManager.I.ConfigReader.GetConfig<BuildingConfig>(furniture.id);
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