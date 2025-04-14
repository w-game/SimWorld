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

    public enum CellType
    {
        Empty,
        Room,
        Wall,
        Door,
        Window,
        Commercial
    }

    public class Room
    {
        public Vector2Int Pos { get; private set; } // 房间位置
        public Vector2Int Size { get; private set; } // 房间大小，影响房间半径
        private System.Random _chunkRand;
        public bool HasLeftWall { get; private set; } = true;
        public bool HasRightWall { get; private set; } = true;
        public bool HasTopWall { get; private set; } = true;
        public bool HasBottomWall { get; private set; } = true;

        public Room(Vector2Int pos, Vector2Int size, System.Random chunkRand,
                    bool left = true, bool right = true, bool top = true, bool bottom = true)
        {
            Pos = pos;
            _chunkRand = chunkRand;
            Size = size;
            HasLeftWall = left;
            HasRightWall = right;
            HasTopWall = top;
            HasBottomWall = bottom;
        }

        public static Dictionary<Vector2Int, CellType> SplitBSPRooms(Vector2Int origin, Vector2Int size, System.Random rand, int depth = 0)
        {
            Dictionary<Vector2Int, CellType> cellMap = new Dictionary<Vector2Int, CellType>();

            return cellMap;
        }
    }

    public class House
    {
        public List<Vector2Int> Blocks { get; private set; } // 房屋的所有块
        public Vector2Int Size { get; private set; } // 房屋大小，影响房屋半径
        public City City { get; private set; } // 房屋所在的城市
        public HouseType HouseType { get; private set; } // 房屋类型

        public Dictionary<Vector2Int, CellType> CellMap { get; private set; } = new(); // 格子类型映射
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

            foreach (var block in blocks)
            {
                City.OriginChunk.AreaTypes.Add(block, HouseType); // 添加到城市的区域类型
            }
        }

        private void CalcRooms()
        {
            for (int i = 0; i < RoomConfig.width; i++)
            {
                for (int j = 0; j < RoomConfig.height; j++)
                {
                    var pos = new Vector2Int(i, j);
                    if (RoomConfig.layout[i + j * RoomConfig.width] == 0)
                    {
                        CellMap.Add(pos, CellType.Wall);
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 1)
                    {
                        CellMap.Add(pos, CellType.Room);
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 2)
                    {
                        CellMap.Add(pos, CellType.Door);
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 3)
                    {
                        CellMap.Add(pos, CellType.Window);
                    }
                    else if (RoomConfig.layout[i + j * RoomConfig.width] == 4)
                    {
                        CellMap.Add(pos, CellType.Commercial);
                        CommercialPos.Add(pos);
                    }
                    else
                    {
                        CellMap.Add(pos, CellType.Empty);
                    }
                }
            }

            foreach (var furniture in RoomConfig.furnitures)
            {
                var pos = new Vector2Int(furniture.pos[0], furniture.pos[1]) + MinPos;

                var config = GameManager.I.ConfigReader.GetConfig<BuildingConfig>(furniture.id);
                Debug.Log($"生成家具: {config.name}，位置: {pos}, 类型: {config.type}");
                var type = Type.GetType($"GameItem.{config.type}Item");
                var furnitureItem = Activator.CreateInstance(type, new object[] { config, new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0) }) as FurnitureItem;
                FurnitureItems.Add(pos, furnitureItem);
            }
        }

        internal float DistanceTo(Vector3 pos)
        {
            return Vector2.Distance(new Vector2(pos.x, pos.y), new Vector2(MinPos.x, MinPos.y));
        }
    }
}