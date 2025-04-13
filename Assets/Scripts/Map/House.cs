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

        public Dictionary<Vector2Int, string> Furnitures { get; private set; } = new Dictionary<Vector2Int, string>();
        public Dictionary<Vector2Int, CellType> CellMap { get; private set; } = new(); // 格子类型映射
        public Vector2 RandomPos => new Vector2(_chunkRand.Next(MinPos.x + 1, MinPos.x + Size.x - 2), _chunkRand.Next(MinPos.y + 1, MinPos.y + Size.y - 2)); // 随机位置

        private System.Random _chunkRand;
        public Vector2Int MinPos { get; private set; } // 房屋最小坐标

        public RoomConfig RoomConfig { get; private set; } // 房间配置

        public House(List<Vector2Int> blocks, RoomConfig config, Vector2Int minPos, City city, System.Random chunkRand)
        {
            Blocks = blocks; // 转换为全局坐标
            RoomConfig = config;
            Size = new Vector2Int(config.width, config.height); // 房屋大小
            MinPos = minPos; // 房屋最小坐标
            City = city;
            _chunkRand = chunkRand;

            CalcHouseType();
            // CalcFurnitures();
            CalcRooms();
        }

        private void CalcFurnitures()
        {
            // if (HouseType != HouseType.House) return;
            // List<string> furnitures = new List<string>()
            // {
            //     "BUILDING_TOILET",
            //     "BUILDING_TABLE",
            //     "BUILDING_STOVE"
            // };

            // foreach (var furniture in furnitures)
            // {
            //     var pos = new Vector2Int(
            //         _chunkRand.Next(MinPos.x + 1, MinPos.x + Size.x - 2),
            //         _chunkRand.Next(MinPos.y + 1, MinPos.y + Size.y - 2));
            //     var config = GameManager.I.ConfigReader.GetConfig<BuildingConfig>(furniture);
            //     Debug.Log($"生成家具: {config.name}，位置: {pos}, 类型: {config.type}");
            //     var type = Type.GetType($"GameItem.{config.type}Item");
            //     var item = Activator.CreateInstance(type, new object[] { config, new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0) }) as GameItemBase;
            //     Furnitures.Add(item);
            // }
        }

        private void CalcHouseType()
        {
            if (HouseType != HouseType.None)
            {
                return;
            }
            var prob = _chunkRand.Next(0, 100);
            if (prob < 60)
            {
                HouseType = HouseType.House;
            }
            else if (prob < 80)
            {
                HouseType = HouseType.Farm;
            }
            else if (prob < 100)
            {
                HouseType = HouseType.Shop;
            }
        }

        private void CalcRooms()
        {
            if (HouseType != HouseType.House)
            {
                return;
            }

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
                    else
                    {
                        CellMap.Add(pos, CellType.Empty);
                    }
                }
            }

            foreach (var furniture in RoomConfig.furnitures)
            {
                var pos = new Vector2Int(furniture.pos[0], furniture.pos[1]) + MinPos;
                Furnitures.Add(pos, furniture.id);
            }
        }
    }
}